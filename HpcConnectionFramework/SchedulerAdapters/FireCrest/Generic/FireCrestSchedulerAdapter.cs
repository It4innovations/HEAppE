using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using HEAppE.Utils;



namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

public class FirecRestSchedulerAdapter : ISchedulerAdapter
{
    #region Instances

    protected ISchedulerDataConvertor _convertor;
    protected ICommands _commands;
    protected ILogger _logger;
    protected HttpClient _httpClient;
    
    public string FirecRestUrl { private get; set; }
    public string TokenEndpoint { private get; set; }

    public string ClientId { private get; set; }
    public string ClientSecret { private get; set; }


    protected static readonly SshTunnelUtils _sshTunnelUtil = new();

    protected static readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    #endregion

    #region Constructors

    public FirecRestSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _commands = null;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
    }

    #endregion

    #region Private Methods

    private string GetAuthToken()
    {
        try
        {
            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
            request.Content = tokenRequestContent;

            var tokenResponse = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter()
                    .GetResult();
                throw new SshCommandException("Failed to obtain OAuth2 token for FirecRest API", errorContent);
            }

            var responseContent = tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter()
                .GetResult();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                return accessTokenElement.GetString();
            }

            throw new SshCommandException("Invalid OAuth2 response", "Access token not found in response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to retrieve FirecRest authentication token: {ex.Message}");
            throw new SshCommandException("Failed to retrieve FirecRest authentication token", ex.Message);
        }
    }

    private void CreateDirectory(string endpoint, string token, string directoryPath, object requestBody)
    {
        var jsonContent = JsonSerializer.Serialize(requestBody);

        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug($"[CreateDirectory] SUCCESS: Directory created: {directoryPath}");
            return;
        }

        _logger.LogDebug($"[CreateDirectory] FAILURE DETECTED. Analyzing error...");

        if ((response.StatusCode == HttpStatusCode.BadRequest ||
             response.StatusCode == HttpStatusCode.InternalServerError ||
             response.StatusCode == HttpStatusCode.NotFound) &&
            (responseContent.Contains("File exists") || responseContent.Contains("directory already exists")))
        {
            _logger.LogDebug($"[CreateDirectory] Directory already exists (safe to ignore): {directoryPath}");
            return;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError ||
            response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug(
                $"[CreateDirectory] Status {response.StatusCode} suggests missing parent or permission issue. Triggering recursion.");

            var parentDirectory = Path.GetDirectoryName(directoryPath.TrimEnd('/'))?.Replace("\\", "/");
            _logger.LogDebug($"[CreateDirectory] Calculated Parent Directory: {parentDirectory}");

            if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/" && parentDirectory != ".")
            {
                try
                {
                    var jsonNode = JsonNode.Parse(jsonContent);
                    if (jsonNode != null)
                    {
                        var updatedRequestBody = jsonNode.DeepClone();
                        updatedRequestBody["path"] = parentDirectory;

                        _logger.LogDebug($"[CreateDirectory] RECURSION: Creating parent {parentDirectory}...");
                        CreateDirectory(endpoint, token, parentDirectory, updatedRequestBody);
                        _logger.LogDebug($"[CreateDirectory] RECURSION DONE. Parent {parentDirectory} handled.");

                        _logger.LogDebug($"[CreateDirectory] RETRYING original: {directoryPath}");
                        using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        retryRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var retryResponse = _httpClient.SendAsync(retryRequest).ConfigureAwait(false).GetAwaiter()
                            .GetResult();
                        var retryContent = retryResponse.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter()
                            .GetResult();

                        _logger.LogDebug($"[CreateDirectory] RETRY Status: {retryResponse.StatusCode}");
                        _logger.LogDebug($"[CreateDirectory] RETRY Content: {retryContent}");

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            _logger.LogDebug($"[CreateDirectory] RETRY SUCCESS: Directory created: {directoryPath}");
                            return;
                        }

                        if ((retryResponse.StatusCode == HttpStatusCode.BadRequest ||
                             retryResponse.StatusCode == HttpStatusCode.InternalServerError ||
                             retryResponse.StatusCode == HttpStatusCode.NotFound) &&
                            (retryContent.Contains("File exists") || retryContent.Contains("directory already exists")))
                        {
                            _logger.LogDebug(
                                $"[CreateDirectory] RETRY indicates directory now exists: {directoryPath}");
                            return;
                        }

                        _logger.LogDebug($"[CreateDirectory] RETRY FAILED. Throwing exception.");
                        throw new FirecrestApiException(
                            $"Failed to create directory (retry): {directoryPath}. Status: {retryResponse.StatusCode}, Response: {retryContent}",
                            retryResponse.StatusCode, retryContent);
                    }
                }
                catch (FirecrestApiException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"[CreateDirectory] EXCEPTION during recursion logic: {ex.Message}");
                    _logger.LogDebug($"[CreateDirectory] Stack Trace: {ex.StackTrace}");
                    throw new FirecrestApiException(
                        $"Failed to create directory during recursion: {directoryPath}. Original error: {responseContent}. Recursive error: {ex.Message}",
                        response.StatusCode, responseContent);
                }
            }
            else
            {
                _logger.LogDebug($"[CreateDirectory] Parent directory was null/root. Cannot recurse further.");
            }
        }

        _logger.LogDebug($"[CreateDirectory] FATAL FAILURE. Throwing exception for: {directoryPath}");
        throw new FirecrestApiException(
            $"Failed to create directory: {directoryPath}. Status: {response.StatusCode}. Response: {responseContent}",
            response.StatusCode, responseContent);
    }

    #endregion

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        try
        {
            _logger.LogDebug($"[SubmitJob] STARTING... JobId: {jobSpecification.Id}, Name: {jobSpecification.Name}");
            var token = GetAuthToken();

            string clusterName = jobSpecification.Cluster.Name;
            string account = jobSpecification.ClusterUser?.Username ?? "default";
            _logger.LogDebug($"[SubmitJob] Target Cluster: {clusterName}, Account: {account}");

            var tasksToQuery = new List<SubmittedTaskInfo>();
            var failedTasks = new List<SubmittedTaskInfo>();

            _logger.LogDebug($"[SubmitJob] Found {jobSpecification.Tasks.Count} tasks to submit.");

            var tasks = (List<(TaskSpecification, string)>)_convertor.ConvertJobSpecificationToJob(jobSpecification, "");

            foreach (var (taskSpec, finalScript) in tasks)
            {
                try
                {
                    string taskDirectoryPath = FileSystemUtils.GetTaskClusterDirectoryPath(taskSpec, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");

                    var jobPayload = new
                    {
                        job = new
                        {
                            name = $"{jobSpecification.Name}-{taskSpec.Id}",
                            working_directory = taskDirectoryPath,
                            account = jobSpecification.Project?.AccountingString,
                            standard_output = taskSpec.StandardOutputFile,
                            standard_error = taskSpec.StandardErrorFile,
                            script = finalScript.Replace("\r\n", "\n")
                        }
                    };

                    var jsonSubmitContent = JsonSerializer.Serialize(jobPayload);
                    var submitEndpoint = $"{FirecRestUrl}/compute/{clusterName}/jobs";

                    using var submitContent = new StringContent(jsonSubmitContent, Encoding.UTF8, "application/json");
                    using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitEndpoint);
                    submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    submitRequest.Content = submitContent;

                    var submitResponse = _httpClient.SendAsync(submitRequest).ConfigureAwait(false).GetAwaiter()
                        .GetResult();

                    var submitResponseContent = submitResponse.Content.ReadAsStringAsync().ConfigureAwait(false)
                        .GetAwaiter().GetResult();

                    if (!submitResponse.IsSuccessStatusCode)
                    {
                        _logger.LogDebug($"[SubmitJob] ERROR: Request failed for Task {taskSpec.Id}");
                        failedTasks.Add(new SubmittedTaskInfo
                        {
                            Name = taskSpec.Id.ToString(),
                            State = TaskState.Failed,
                            Specification = taskSpec,
                            Reason =
                                $"Job submission request failed with status {submitResponse.StatusCode}. Response: {submitResponseContent}"
                        });
                        continue;
                    }

                    _logger.LogDebug($"[SubmitJob] Parsing Job ID from response...");
                    var jobIds = _convertor.GetJobIds(submitResponseContent);
                    var submittedJobId = jobIds.FirstOrDefault();

                    if (submittedJobId != null)
                    {
                        _logger.LogDebug($"[SubmitJob] SUCCESS! FirecREST Job ID found: {submittedJobId}");
                        tasksToQuery.Add(new SubmittedTaskInfo
                        {
                            ScheduledJobId = submittedJobId,
                            Specification = taskSpec,
                            Name = taskSpec.Id.ToString()
                        });
                    }
                    else
                    {
                        _logger.LogDebug($"[SubmitJob] ERROR: Could not parse Job ID.");
                        failedTasks.Add(new SubmittedTaskInfo
                        {
                            Name = taskSpec.Id.ToString(),
                            State = TaskState.Failed,
                            Specification = taskSpec,
                            Reason =
                                $"Job submission response did not contain a parsable job ID. Response: {submitResponseContent}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"[SubmitJob] EXCEPTION inside loop: {ex}");
                    failedTasks.Add(new SubmittedTaskInfo
                    {
                        Name = taskSpec.Id.ToString(), State = TaskState.Failed, Specification = taskSpec,
                        Reason = $"An exception occurred during submission process: {ex.Message}"
                    });
                }
            }

            var finalSubmittedTasks = new List<SubmittedTaskInfo>(failedTasks);

            if (tasksToQuery.Any())
            {
                _logger.LogDebug($"[SubmitJob] Querying actual status for {tasksToQuery.Count} tasks...");
                var queriedTasks = await GetActualTasksInfo(connectorClient, jobSpecification.Cluster, tasksToQuery, null);
                finalSubmittedTasks.AddRange(queriedTasks);
            }

            return finalSubmittedTasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An unhandled error occurred in SubmitJob: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string key)
    {

        await Task.Delay(1);

        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        Task.Run(async () =>
        {
            var token = GetAuthToken();

            foreach (var task in submittedTasksInfo)
            {
                if (string.IsNullOrEmpty(task.ScheduledJobId))
                {
                    _logger.LogDebug($"[GetActualTasksInfo] Skipping task {task.Name} (No ScheduledJobId).");
                    continue;
                }

                try
                {
                    var endpoint = $"{FirecRestUrl}/compute/{cluster.Name}/jobs/{task.ScheduledJobId}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug($"[GetActualTasksInfo] Parsing response for Job {task.ScheduledJobId}...");
                        var taskInfoList = _convertor.ReadParametersFromResponse(cluster, responseContent);

                        if (taskInfoList?.FirstOrDefault() is { } newInfo)
                        {
                            _logger.LogDebug(
                                $"[GetActualTasksInfo] SUCCESS. Updating Task {task.Name} state to: {newInfo.State}");
                            task.State = newInfo.State;
                            task.StartTime = newInfo.StartTime;
                            task.EndTime = newInfo.EndTime;
                            task.AllocatedTime = newInfo.AllocatedTime;
                            task.TaskAllocationNodes = newInfo.TaskAllocationNodes;
                        }
                        else
                        {
                            _logger.LogDebug(
                                $"[GetActualTasksInfo] WARNING: Parser returned null or empty list for Job {task.ScheduledJobId}");
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"[GetActualTasksInfo] ERROR: Request failed for Job {task.ScheduledJobId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking status for job {task.ScheduledJobId}: {ex.Message}");
                    throw;
                }
            }
        }).GetAwaiter().GetResult();

        return submittedTasksInfo;
    }

    public async Task CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string message)
    {
        await Task.Delay(1);

        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            throw new ArgumentException("Cannot cancel jobs: The provided list of tasks is null or empty.",
                nameof(submittedTasksInfo));
        }

        try
        {
            var token = GetAuthToken();
            var tasksToCancel = submittedTasksInfo
                .Where(task => !string.IsNullOrEmpty(task.ScheduledJobId))
                .ToList();

            _logger.LogDebug($"[CancelJob] Found {tasksToCancel.Count} tasks with valid ScheduledJobId to cancel.");

            var cancellationTasks = tasksToCancel.Select(async task =>
            {
                string clusterName = task.Specification.JobSpecification.Cluster.Name;
                var endpoint = $"{FirecRestUrl}/compute/{clusterName}/jobs/{task.ScheduledJobId}";

                _logger.LogDebug(
                    $"[CancelJob] Sending DELETE request for Job {task.ScheduledJobId} on {clusterName}...");
                _logger.LogDebug($"[CancelJob] Endpoint: {endpoint}");

                using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    var response = await _httpClient.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug(
                        $"[CancelJob] Response for Job {task.ScheduledJobId}: Status={response.StatusCode}, Content={content}");

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug($"[CancelJob] WARNING: Failed to cancel Job {task.ScheduledJobId}");
                    }

                    return response;
                }
                catch (Exception innerEx)
                {
                    _logger.LogDebug($"[CancelJob] EXCEPTION canceling Job {task.ScheduledJobId}: {innerEx.Message}");
                    throw;
                }
            }).ToList();

            if (cancellationTasks.Any())
            {
                _logger.LogDebug(
                    $"[CancelJob] Waiting for {cancellationTasks.Count} cancellation requests to complete...");
                Task.WhenAll(cancellationTasks).GetAwaiter().GetResult();
                _logger.LogDebug("[CancelJob] All cancellation requests completed.");
            }
            else
            {
                _logger.LogDebug(
                    "[CancelJob] WARNING: No valid tasks found to cancel (ScheduledJobId was missing for all inputs).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while canceling jobs in parallel: {ex.Message}");
            throw new Exception("Failed to cancel jobs via FirecRest API. See inner exception for details.", ex);
        }
    }

    public async Task CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        try
        {
            await Task.Delay(1);
            var token = GetAuthToken();

            string clusterName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            string jobDirectoryPath = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");

            var endpoint = $"{FirecRestUrl}/filesystem/{clusterName}/ops/mkdir";
            var jobRequestBody = new { path = jobDirectoryPath, p = true };

            CreateDirectory(endpoint, token, jobDirectoryPath, jobRequestBody);

            _logger.LogDebug($"[CreateJobDirectory] Found {jobInfo.Tasks.Count} tasks. Creating subdirectories...");

            foreach (var task in jobInfo.Tasks)
            {
                string taskDirectoryPath = $"{jobDirectoryPath}/{task.Specification.Id}".Replace("\\", "/");
                var taskRequestBody = new { path = taskDirectoryPath, p = true };
                CreateDirectory(endpoint, token, taskDirectoryPath, taskRequestBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating job directory: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        try
        {
            await Task.Delay(1);
            var token = GetAuthToken();

            string systemName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            string remotePathToDelete = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");

            var endpoint =
                $"{FirecRestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(remotePathToDelete)}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _logger.LogWarning(
                    $"Failed to delete job directory for Job ID {jobInfo.Id}. Status: {response.StatusCode}. Response: {errorContent}");
                return false;
            }
            else
            {
                _logger.LogInformation(
                    $"Successfully received response for job directory deletion for Job ID {jobInfo.Id}. Status: {response.StatusCode}.");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An exception occurred while deleting job directory for Job ID {jobInfo.Id}: {ex.Message}");
            return false;
        }
    }

    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
    {
        await Task.Delay(1);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        await Task.Delay(1);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath) =>
        await _commands?.GetParametersFromGenericUserScriptAsync(connectorClient, userScriptPath);

    public async Task AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo) =>
        await _commands?.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);

    public async Task RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString) =>
        await _commands?.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);

    public async Task CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path) =>
        await _commands?.CopyJobDataToTempAsync(connectorClient, jobInfo, localBasePath, hash, path);

    public async Task CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash) =>
        await _commands?.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);

    public async Task CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort) =>
        await _sshTunnelUtil.CreateTunnelAsync(connectorClient, taskInfo.Id, nodeHost, nodePort);

    public async Task RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo) =>
        await _sshTunnelUtil.RemoveTunnelAsync(connectorClient, taskInfo.Id);

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost) =>
        _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);

    public async Task<bool> InitializeClusterScriptDirectory(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        string account, bool isServiceAccount) => await _commands?.InitializeClusterScriptDirectoryAsync(
        schedulerConnectionConnection, clusterProjectRootDirectory, overwriteExistingProjectRootDirectory,
        localBasepath, account, isServiceAccount);

    public async Task<bool> MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode) =>
        await _commands?.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);

    public Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog) =>
        throw new NotImplementedException();

    public Task<DryRunJobInfo> DryRunJob(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification) =>
        throw new NotImplementedException();
}
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
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

public class FireCrestSchedulerAdapter : ISchedulerAdapter
{
    #region Instances

    protected ISchedulerDataConvertor _convertor;
    protected ICommands _commands;
    protected ILog _log;
    protected HttpClient _httpClient;
    protected string _firecrestUrl;
    protected string _baseDirectoryPath;
    protected string _tokenEndpoint;
    protected string _clientId;
    protected string _clientSecret;
    protected static SshTunnelUtils _sshTunnelUtil = new SshTunnelUtils();

    #endregion

    #region Constructors

    public FireCrestSchedulerAdapter(ISchedulerDataConvertor convertor)
    {
        _log = LogManager.GetLogger(typeof(FireCrestSchedulerAdapter));
        _convertor = convertor;
        _commands = new LinuxCommands();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        _firecrestUrl = FireCrestSettings.FireCrestUrl;
        _baseDirectoryPath = FireCrestSettings.BaseDirectoryPath;
        _tokenEndpoint = FireCrestSettings.TokenEndpoint;
        _clientId = FireCrestSettings.ClientId;
        _clientSecret = FireCrestSettings.ClientSecret;
    }

    #endregion

    #region Private Methods

    private string GetAuthTokenAsync()
    {
        try
        {
            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            request.Content = tokenRequestContent;

            var tokenResponse = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = tokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter()
                    .GetResult();
                throw new SshCommandException("Failed to obtain OAuth2 token for FireCrest API", errorContent);
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
            _log.Error($"Failed to retrieve FireCrest authentication token: {ex.Message}", ex);
            throw new SshCommandException("Failed to retrieve FireCrest authentication token", ex.Message);
        }
    }

    private void CreateDirectory(string endpoint, string token, string directoryPath, object requestBody)
    {
        Console.WriteLine("==========================================================================================");
        Console.WriteLine($"[CreateDirectory] START. Path: {directoryPath}");
        Console.WriteLine($"[CreateDirectory] Endpoint: {endpoint}");

        var jsonContent = JsonSerializer.Serialize(requestBody);
        Console.WriteLine($"[CreateDirectory] Payload: {jsonContent}");

        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        Console.WriteLine($"[CreateDirectory] Sending HTTP Request...");
        var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();

        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        Console.WriteLine($"[CreateDirectory] Response Status: {response.StatusCode}");
        Console.WriteLine($"[CreateDirectory] Response Content: {responseContent}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[CreateDirectory] SUCCESS: Directory created: {directoryPath}");
            return;
        }

        Console.WriteLine($"[CreateDirectory] FAILURE DETECTED. Analyzing error...");

        if ((response.StatusCode == HttpStatusCode.BadRequest ||
             response.StatusCode == HttpStatusCode.InternalServerError ||
             response.StatusCode == HttpStatusCode.NotFound) &&
            (responseContent.Contains("File exists") || responseContent.Contains("directory already exists")))
        {
            Console.WriteLine($"[CreateDirectory] Directory already exists (safe to ignore): {directoryPath}");
            return;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError ||
            response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine(
                $"[CreateDirectory] Status {response.StatusCode} suggests missing parent or permission issue. Triggering recursion.");

            var parentDirectory = Path.GetDirectoryName(directoryPath.TrimEnd('/'))?.Replace("\\", "/");
            Console.WriteLine($"[CreateDirectory] Calculated Parent Directory: {parentDirectory}");

            if (!string.IsNullOrEmpty(parentDirectory) && parentDirectory != "/" && parentDirectory != ".")
            {
                try
                {
                    var jsonNode = JsonNode.Parse(jsonContent);
                    if (jsonNode != null)
                    {
                        var updatedRequestBody = jsonNode.DeepClone();
                        updatedRequestBody["path"] = parentDirectory;

                        Console.WriteLine($"[CreateDirectory] RECURSION: Creating parent {parentDirectory}...");
                        CreateDirectory(endpoint, token, parentDirectory, updatedRequestBody);
                        Console.WriteLine($"[CreateDirectory] RECURSION DONE. Parent {parentDirectory} handled.");

                        Console.WriteLine($"[CreateDirectory] RETRYING original: {directoryPath}");
                        using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        retryRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var retryResponse = _httpClient.SendAsync(retryRequest).ConfigureAwait(false).GetAwaiter()
                            .GetResult();
                        var retryContent = retryResponse.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter()
                            .GetResult();

                        Console.WriteLine($"[CreateDirectory] RETRY Status: {retryResponse.StatusCode}");
                        Console.WriteLine($"[CreateDirectory] RETRY Content: {retryContent}");

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[CreateDirectory] RETRY SUCCESS: Directory created: {directoryPath}");
                            return;
                        }

                        if ((retryResponse.StatusCode == HttpStatusCode.BadRequest ||
                             retryResponse.StatusCode == HttpStatusCode.InternalServerError ||
                             retryResponse.StatusCode == HttpStatusCode.NotFound) &&
                            (retryContent.Contains("File exists") || retryContent.Contains("directory already exists")))
                        {
                            Console.WriteLine(
                                $"[CreateDirectory] RETRY indicates directory now exists: {directoryPath}");
                            return;
                        }

                        Console.WriteLine($"[CreateDirectory] RETRY FAILED. Throwing exception.");
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
                    Console.WriteLine($"[CreateDirectory] EXCEPTION during recursion logic: {ex.Message}");
                    Console.WriteLine($"[CreateDirectory] Stack Trace: {ex.StackTrace}");
                    throw new FirecrestApiException(
                        $"Failed to create directory during recursion: {directoryPath}. Original error: {responseContent}. Recursive error: {ex.Message}",
                        response.StatusCode, responseContent);
                }
            }
            else
            {
                Console.WriteLine($"[CreateDirectory] Parent directory was null/root. Cannot recurse further.");
            }
        }

        Console.WriteLine($"[CreateDirectory] FATAL FAILURE. Throwing exception for: {directoryPath}");
        throw new FirecrestApiException(
            $"Failed to create directory: {directoryPath}. Status: {response.StatusCode}. Response: {responseContent}",
            response.StatusCode, responseContent);
    }

    #endregion

    public IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        try
        {
            Console.WriteLine($"[SubmitJob] STARTING... JobId: {jobSpecification.Id}, Name: {jobSpecification.Name}");
            Console.WriteLine("[SubmitJob] Requesting Auth Token...");
            var token = GetAuthTokenAsync();
            Console.WriteLine($"[SubmitJob] Token retrieved. Length: {token?.Length ?? 0}");

            string clusterName = jobSpecification.Cluster.Name;
            string account = jobSpecification.ClusterUser?.Username ?? "default";
            Console.WriteLine($"[SubmitJob] Target Cluster: {clusterName}, Account: {account}");

            var tasksToQuery = new List<SubmittedTaskInfo>();
            var failedTasks = new List<SubmittedTaskInfo>();

            Console.WriteLine($"[SubmitJob] Found {jobSpecification.Tasks.Count} tasks to submit.");

            foreach (var taskSpec in jobSpecification.Tasks)
            {
                try
                {
                    Console.WriteLine("----------------------------------------------------------------");
                    Console.WriteLine($"[SubmitJob] Processing Task ID: {taskSpec.Id}");

                    Console.WriteLine($"[SubmitJob] Converting script for Task {taskSpec.Id}...");
                    var finalScript = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, taskSpec);
                    Console.WriteLine($"[SubmitJob] Script converted. Length: {finalScript?.Length ?? 0}");

                    string taskDirectoryPath =
                        $"{_baseDirectoryPath}/{account}/{jobSpecification.Id}/{taskSpec.Id}".Replace("\\", "/");
                    Console.WriteLine($"[SubmitJob] Task Directory: {taskDirectoryPath}");

                    var jobPayload = new
                    {
                        job = new
                        {
                            name = $"{jobSpecification.Name}-{taskSpec.Id}",
                            working_directory = taskDirectoryPath,
                            account = jobSpecification.Project?.AccountingString,
                            standard_output = taskSpec.StandardOutputFile,
                            standard_error = taskSpec.StandardErrorFile,
                            script = finalScript                        }
                    };

                    var jsonSubmitContent = JsonSerializer.Serialize(jobPayload);
                    Console.WriteLine($"[SubmitJob] PAYLOAD TO SEND: {jsonSubmitContent}");

                    var submitEndpoint = $"{_firecrestUrl}/compute/{clusterName}/jobs";
                    Console.WriteLine($"[SubmitJob] Sending POST to: {submitEndpoint}");

                    using var submitContent = new StringContent(jsonSubmitContent, Encoding.UTF8, "application/json");
                    using var submitRequest = new HttpRequestMessage(HttpMethod.Post, submitEndpoint);
                    submitRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    submitRequest.Content = submitContent;

                    var submitResponse = _httpClient.SendAsync(submitRequest).ConfigureAwait(false).GetAwaiter()
                        .GetResult();

                    var submitResponseContent = submitResponse.Content.ReadAsStringAsync().ConfigureAwait(false)
                        .GetAwaiter().GetResult();

                    Console.WriteLine($"[SubmitJob] RESPONSE STATUS: {submitResponse.StatusCode}");
                    Console.WriteLine($"[SubmitJob] RESPONSE BODY: {submitResponseContent}");

                    if (!submitResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[SubmitJob] ERROR: Request failed for Task {taskSpec.Id}");
                        failedTasks.Add(new SubmittedTaskInfo
                        {
                            Name = taskSpec.Id.ToString(), State = TaskState.Failed, Specification = taskSpec,
                            Reason =
                                $"Job submission request failed with status {submitResponse.StatusCode}. Response: {submitResponseContent}"
                        });
                        continue;
                    }

                    Console.WriteLine($"[SubmitJob] Parsing Job ID from response...");
                    var jobIds = _convertor.GetJobIds(submitResponseContent);
                    var submittedJobId = jobIds.FirstOrDefault();

                    if (submittedJobId != null)
                    {
                        Console.WriteLine($"[SubmitJob] SUCCESS! FirecREST Job ID found: {submittedJobId}");
                        tasksToQuery.Add(new SubmittedTaskInfo
                        {
                            ScheduledJobId = submittedJobId,
                            Specification = taskSpec,
                            Name = taskSpec.Id.ToString()
                        });
                    }
                    else
                    {
                        Console.WriteLine($"[SubmitJob] ERROR: Could not parse Job ID.");
                        failedTasks.Add(new SubmittedTaskInfo
                        {
                            Name = taskSpec.Id.ToString(), State = TaskState.Failed, Specification = taskSpec,
                            Reason =
                                $"Job submission response did not contain a parsable job ID. Response: {submitResponseContent}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SubmitJob] EXCEPTION inside loop: {ex}");
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
                Console.WriteLine($"[SubmitJob] Querying actual status for {tasksToQuery.Count} tasks...");
                var queriedTasks = GetActualTasksInfo(connectorClient, jobSpecification.Cluster, tasksToQuery, null);
                finalSubmittedTasks.AddRange(queriedTasks);
            }

            Console.WriteLine($"[SubmitJob] FINISHED. Total results: {finalSubmittedTasks.Count}");
            return finalSubmittedTasks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SubmitJob] CRITICAL FATAL EXCEPTION: {ex}");
            _log.Error($"An unhandled error occurred in SubmitJob: {ex.Message}", ex);
            throw;
        }
    }

    public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string key)
    {
        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            Console.WriteLine("[GetActualTasksInfo] Input list is empty or null. Returning empty.");
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        Console.WriteLine(
            $"[GetActualTasksInfo] START. Querying {submittedTasksInfo.Count()} tasks for Cluster: {cluster.Name}");

        Task.Run(async () =>
        {
            Console.WriteLine("[GetActualTasksInfo] Getting Auth Token...");
            var token = GetAuthTokenAsync();
            Console.WriteLine("[GetActualTasksInfo] Token retrieved.");

            foreach (var task in submittedTasksInfo)
            {
                if (string.IsNullOrEmpty(task.ScheduledJobId))
                {
                    Console.WriteLine($"[GetActualTasksInfo] Skipping task {task.Name} (No ScheduledJobId).");
                    continue;
                }

                try
                {
                    var endpoint = $"{_firecrestUrl}/compute/{cluster.Name}/jobs/{task.ScheduledJobId}";
                    Console.WriteLine($"[GetActualTasksInfo] ------------------------------------------------");
                    Console.WriteLine($"[GetActualTasksInfo] Querying Job ID: {task.ScheduledJobId}");
                    Console.WriteLine($"[GetActualTasksInfo] Endpoint: {endpoint}");

                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"[GetActualTasksInfo] Response Status: {response.StatusCode}");
                    Console.WriteLine($"[GetActualTasksInfo] Response Content: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[GetActualTasksInfo] Parsing response for Job {task.ScheduledJobId}...");
                        var taskInfoList = _convertor.ReadParametersFromResponse(cluster, responseContent);

                        if (taskInfoList?.FirstOrDefault() is { } newInfo)
                        {
                            Console.WriteLine(
                                $"[GetActualTasksInfo] SUCCESS. Updating Task {task.Name} state to: {newInfo.State}");
                            task.State = newInfo.State;
                            task.StartTime = newInfo.StartTime;
                            task.EndTime = newInfo.EndTime;
                            task.AllocatedTime = newInfo.AllocatedTime;
                            task.TaskAllocationNodes = newInfo.TaskAllocationNodes;
                        }
                        else
                        {
                            Console.WriteLine(
                                $"[GetActualTasksInfo] WARNING: Parser returned null or empty list for Job {task.ScheduledJobId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[GetActualTasksInfo] ERROR: Request failed for Job {task.ScheduledJobId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GetActualTasksInfo] EXCEPTION for Job {task.ScheduledJobId}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    _log.Error($"Error checking status for job {task.ScheduledJobId}: {ex.Message}", ex);
                }
            }
        }).GetAwaiter().GetResult();

        Console.WriteLine("[GetActualTasksInfo] FINISHED processing all tasks.");
        return submittedTasksInfo;
    }

    public void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string message)
    {
        Console.WriteLine($"[CancelJob] START. Received request to cancel {submittedTasksInfo?.Count() ?? 0} tasks.");

        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            Console.WriteLine("[CancelJob] ERROR: Task list is empty.");
            throw new ArgumentException("Cannot cancel jobs: The provided list of tasks is null or empty.",
                nameof(submittedTasksInfo));
        }

        try
        {
            Console.WriteLine("[CancelJob] Retrieving Auth Token...");
            var token = GetAuthTokenAsync();
            Console.WriteLine("[CancelJob] Token retrieved.");

            var tasksToCancel = submittedTasksInfo
                .Where(task => !string.IsNullOrEmpty(task.ScheduledJobId))
                .ToList();

            Console.WriteLine($"[CancelJob] Found {tasksToCancel.Count} tasks with valid ScheduledJobId to cancel.");

            var cancellationTasks = tasksToCancel.Select(async task =>
            {
                string clusterName = task.Specification.JobSpecification.Cluster.Name;
                var endpoint = $"{_firecrestUrl}/compute/{clusterName}/jobs/{task.ScheduledJobId}";

                Console.WriteLine(
                    $"[CancelJob] Sending DELETE request for Job {task.ScheduledJobId} on {clusterName}...");
                Console.WriteLine($"[CancelJob] Endpoint: {endpoint}");

                using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                try
                {
                    var response = await _httpClient.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();

                    Console.WriteLine(
                        $"[CancelJob] Response for Job {task.ScheduledJobId}: Status={response.StatusCode}, Content={content}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[CancelJob] WARNING: Failed to cancel Job {task.ScheduledJobId}");
                    }

                    return response;
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine($"[CancelJob] EXCEPTION canceling Job {task.ScheduledJobId}: {innerEx.Message}");
                    throw;
                }
            }).ToList();

            if (cancellationTasks.Any())
            {
                Console.WriteLine(
                    $"[CancelJob] Waiting for {cancellationTasks.Count} cancellation requests to complete...");
                Task.WhenAll(cancellationTasks).GetAwaiter().GetResult();
                Console.WriteLine("[CancelJob] All cancellation requests completed.");
            }
            else
            {
                Console.WriteLine(
                    "[CancelJob] WARNING: No valid tasks found to cancel (ScheduledJobId was missing for all inputs).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CancelJob] CRITICAL EXCEPTION: {ex.Message}");
            _log.Error($"An error occurred while canceling jobs in parallel: {ex.Message}", ex);
            throw new Exception("Failed to cancel jobs via FireCrest API. See inner exception for details.", ex);
        }
    }

    public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        Console.WriteLine("################################################################");
        Console.WriteLine($"[CreateJobDirectory] START. Job ID: {jobInfo.Id}");

        try
        {
            Console.WriteLine("[CreateJobDirectory] Retrieving Auth Token...");
            var token = GetAuthTokenAsync();
            Console.WriteLine("[CreateJobDirectory] Token retrieved.");

            string clusterName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            Console.WriteLine($"[CreateJobDirectory] Cluster: {clusterName}");
            Console.WriteLine($"[CreateJobDirectory] Account: {account}");

            string jobDirectoryPath = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}".Replace("\\", "/");
            var endpoint = $"{_firecrestUrl}/filesystem/{clusterName}/ops/mkdir";

            Console.WriteLine($"[CreateJobDirectory] Job Directory Path: {jobDirectoryPath}");
            Console.WriteLine($"[CreateJobDirectory] API Endpoint: {endpoint}");

            Console.WriteLine($"[CreateJobDirectory] >>> Creating MAIN Job Directory...");
            var jobRequestBody = new { path = jobDirectoryPath, p = true };

            CreateDirectory(endpoint, token, jobDirectoryPath, jobRequestBody);
            Console.WriteLine($"[CreateJobDirectory] <<< Main directory creation returned.");

            Console.WriteLine($"[CreateJobDirectory] Found {jobInfo.Tasks.Count} tasks. Creating subdirectories...");

            foreach (var task in jobInfo.Tasks)
            {
                string taskDirectoryPath = $"{jobDirectoryPath}/{task.Specification.Id}".Replace("\\", "/");
                Console.WriteLine($"[CreateJobDirectory] >>> Creating Task Directory: {taskDirectoryPath}");

                var taskRequestBody = new { path = taskDirectoryPath, p = true };
                CreateDirectory(endpoint, token, taskDirectoryPath, taskRequestBody);
            }

            Console.WriteLine("[CreateJobDirectory] SUCCESS: All job directories created successfully.");
            Console.WriteLine("################################################################");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateJobDirectory] CRITICAL EXCEPTION: {ex.Message}");
            Console.WriteLine($"[CreateJobDirectory] Stack Trace: {ex.StackTrace}");
            _log.Error($"Error creating job directory: {ex.Message}", ex);
            throw;
        }
    }

    public bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine($"[DeleteJobDirectory] START. Attempting to delete directory for Job ID: {jobInfo.Id}");

        try
        {
            Console.WriteLine("[DeleteJobDirectory] Retrieving Auth Token...");
            var token = GetAuthTokenAsync();
            Console.WriteLine("[DeleteJobDirectory] Token retrieved.");

            string systemName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            string remotePathToDelete = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}".Replace("\\", "/");

            Console.WriteLine($"[DeleteJobDirectory] Cluster: {systemName}");
            Console.WriteLine($"[DeleteJobDirectory] Account: {account}");
            Console.WriteLine($"[DeleteJobDirectory] Target Path: {remotePathToDelete}");

            var endpoint =
                $"{_firecrestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(remotePathToDelete)}";
            Console.WriteLine($"[DeleteJobDirectory] API Endpoint: {endpoint}");

            Console.WriteLine($"[DeleteJobDirectory] Sending DELETE request...");

            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();

            Console.WriteLine($"[DeleteJobDirectory] Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                Console.WriteLine($"[DeleteJobDirectory] ERROR: Failed to delete directory.");
                Console.WriteLine($"[DeleteJobDirectory] Error Content: {errorContent}");

                _log.Warn(
                    $"Failed to delete job directory for Job ID {jobInfo.Id}. Status: {response.StatusCode}. Response: {errorContent}");
                return false;
            }
            else
            {
                Console.WriteLine($"[DeleteJobDirectory] SUCCESS: Directory deleted.");
                _log.Info(
                    $"Successfully received response for job directory deletion for Job ID {jobInfo.Id}. Status: {response.StatusCode}.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteJobDirectory] CRITICAL EXCEPTION: {ex.Message}");
            Console.WriteLine($"[DeleteJobDirectory] Stack Trace: {ex.StackTrace}");

            _log.Error($"An exception occurred while deleting job directory for Job ID {jobInfo.Id}: {ex.Message}", ex);
            return false;
        }
        finally
        {
            Console.WriteLine("[DeleteJobDirectory] END.");
            Console.WriteLine("----------------------------------------------------------------");
        }
    }

    public ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType) =>
        throw new NotImplementedException();

    public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo) =>
        throw new NotImplementedException();

    public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath) =>
        _commands.GetParametersFromGenericUserScript(connectorClient, userScriptPath);

    public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo) =>
        _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);

    public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys,
        string projectAccountingString) =>
        _commands.RemoveDirectFileTransferAccessForUser(connectorClient, publicKeys, projectAccountingString);

    public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path) => _commands.CopyJobDataToTemp(connectorClient, jobInfo, localBasePath, hash, path);

    public void
        CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash) =>
        _commands.CopyJobDataFromTemp(connectorClient, jobInfo, localBasePath, hash);

    public void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort) =>
        _sshTunnelUtil.CreateTunnel(connectorClient, taskInfo.Id, nodeHost, nodePort);

    public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo) =>
        _sshTunnelUtil.RemoveTunnel(connectorClient, taskInfo.Id);

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost) =>
        _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);

    public bool InitializeClusterScriptDirectory(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        string account, bool isServiceAccount) => _commands.InitializeClusterScriptDirectory(
        schedulerConnectionConnection, clusterProjectRootDirectory, overwriteExistingProjectRootDirectory,
        localBasepath, account, isServiceAccount);

    public bool MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo,
        IEnumerable<Tuple<string, string>> sourceDestinations) =>
        _commands.CopyJobFiles(schedulerConnectionConnection, jobInfo, sourceDestinations);
}
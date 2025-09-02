using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
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
    protected static SshTunnelUtils _sshTunnelUtil = new SshTunnelUtils();

    #endregion

    #region Constructors

    public FireCrestSchedulerAdapter(ISchedulerDataConvertor convertor)
    {
        _log = LogManager.GetLogger(typeof(FireCrestSchedulerAdapter));
        _convertor = convertor;
        _commands = new LinuxCommands();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        _firecrestUrl = "http://host.docker.internal:8000";
        _baseDirectoryPath = "/home/fireuser/Identifier/HEAppE/Executions";
    }

    #endregion

    #region Private Methods

    private string GetAuthToken(object connectorClient)
    {
        try
        {
            var tokenEndpoint = "http://host.docker.internal:8080/auth/realms/kcrealm/protocol/openid-connect/token";
            var clientId = "firecrest-test-client";
            var clientSecret = "wZVHVIEd9dkJDh9hMKc6DTvkqXxnDttk";

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
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
        var jsonContent = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            throw new FirecrestApiException($"Failed to create directory: {directoryPath}", response.StatusCode,
                responseContent);
        }
    }

    #endregion

    public IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        try
        {
            var token = GetAuthToken(connectorClient);
            string clusterName = jobSpecification.Cluster.Name;
            string account = jobSpecification.ClusterUser?.Username ?? "default";

            var tasksToQuery = new List<SubmittedTaskInfo>();
            var failedTasks = new List<SubmittedTaskInfo>();

            foreach (var taskSpec in jobSpecification.Tasks)
            {
                try
                {
                    var finalScript = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, taskSpec);
                    string taskDirectoryPath =
                        $"{_baseDirectoryPath}/{account}/{jobSpecification.Id}/{taskSpec.Id}".Replace("\\", "/");

                    var jobPayload = new
                    {
                        job = new
                        {
                            name = $"{jobSpecification.Name}-{taskSpec.Id}", working_directory = taskDirectoryPath,
                            account = jobSpecification.Project?.AccountingString,
                            standard_output = taskSpec.StandardOutputFile, standard_error = taskSpec.StandardErrorFile,
                            script = finalScript
                        }
                    };

                    var jsonSubmitContent = JsonSerializer.Serialize(jobPayload);
                    var submitEndpoint = $"{_firecrestUrl}/compute/{clusterName}/jobs";

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
                        failedTasks.Add(new SubmittedTaskInfo
                        {
                            Name = taskSpec.Id.ToString(), State = TaskState.Failed, Specification = taskSpec,
                            Reason =
                                $"Job submission request failed with status {submitResponse.StatusCode}. Response: {submitResponseContent}"
                        });
                        continue;
                    }

                    var jobIds = _convertor.GetJobIds(submitResponseContent);
                    var submittedJobId = jobIds.FirstOrDefault();

                    if (submittedJobId != null)
                    {
                        tasksToQuery.Add(new SubmittedTaskInfo
                        {
                            ScheduledJobId = submittedJobId,
                            Specification = taskSpec,
                            Name = taskSpec.Id.ToString()
                        });
                    }
                    else
                    {
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
                var queriedTasks = GetActualTasksInfo(connectorClient, jobSpecification.Cluster, tasksToQuery, null);
                finalSubmittedTasks.AddRange(queriedTasks);
            }

            return finalSubmittedTasks;
        }
        catch (Exception ex)
        {
            _log.Error($"An unhandled error occurred in SubmitJob: {ex.Message}", ex);
            throw;
        }
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var tokenEndpoint = "http://host.docker.internal:8080/auth/realms/kcrealm/protocol/openid-connect/token";
        var clientId = "firecrest-test-client";
        var clientSecret = "wZVHVIEd9dkJDh9hMKc6DTvkqXxnDttk";

        var tokenRequestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = tokenRequestContent;

        var tokenResponse = await _httpClient.SendAsync(request);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            throw new Exception("Failed to obtain OAuth2 token for FireCrest API: " + errorContent);
        }

        var responseContent = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
        {
            return accessTokenElement.GetString();
        }

        throw new Exception("Invalid OAuth2 response, access token not found.");
    }

    public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string key)
    {
        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        Task.Run(async () =>
        {
            var token = await GetAuthTokenAsync();

            foreach (var task in submittedTasksInfo)
            {
                if (string.IsNullOrEmpty(task.ScheduledJobId)) continue;
                try
                {
                    var endpoint = $"{_firecrestUrl}/compute/{cluster.Name}/jobs/{task.ScheduledJobId}";
                    using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var taskInfoList = _convertor.ReadParametersFromResponse(cluster, responseContent);

                        if (taskInfoList?.FirstOrDefault() is { } newInfo)
                        {
                            task.State = newInfo.State;
                            task.StartTime = newInfo.StartTime;
                            task.EndTime = newInfo.EndTime;
                            task.AllocatedTime = newInfo.AllocatedTime;
                            task.TaskAllocationNodes = newInfo.TaskAllocationNodes;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error checking status for job {task.ScheduledJobId}: {ex.Message}", ex);
                }
            }
        }).GetAwaiter().GetResult();

        return submittedTasksInfo;
    }

    public void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string message)
    {
        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            throw new ArgumentException("Cannot cancel jobs: The provided list of tasks is null or empty.",
                nameof(submittedTasksInfo));
        }

        try
        {
            var token = GetAuthToken(connectorClient);

            var cancellationTasks = submittedTasksInfo
                .Where(task => !string.IsNullOrEmpty(task.ScheduledJobId))
                .Select(task =>
                {
                    string clusterName = task.Specification.JobSpecification.Cluster.Name;
                    var endpoint = $"{_firecrestUrl}/compute/{clusterName}/jobs/{task.ScheduledJobId}";

                    var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    return _httpClient.SendAsync(request);
                })
                .ToList();


            if (cancellationTasks.Any())
            {
                Task.WhenAll(cancellationTasks).GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("[CancelJob] WARNING: No tasks with a valid ScheduledJobId were found to cancel.");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"An error occurred while canceling jobs in parallel: {ex.Message}", ex);
            throw new Exception("Failed to cancel jobs via FireCrest API. See inner exception for details.", ex);
        }
    }

    public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        try
        {
            var token = GetAuthToken(connectorClient);
            string clusterName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;
            string jobDirectoryPath = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}".Replace("\\", "/");
            var endpoint = $"{_firecrestUrl}/filesystem/{clusterName}/ops/mkdir";

            var jobRequestBody = new { path = jobDirectoryPath, p = true };
            CreateDirectory(endpoint, token, jobDirectoryPath, jobRequestBody);

            foreach (var task in jobInfo.Tasks)
            {
                string taskDirectoryPath = $"{jobDirectoryPath}/{task.Specification.Id}".Replace("\\", "/");
                var taskRequestBody = new { path = taskDirectoryPath, p = true };
                CreateDirectory(endpoint, token, taskDirectoryPath, taskRequestBody);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Error creating job directory: {ex.Message}", ex);
            throw;
        }
    }

    public bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        try
        {
            _log.Info($"Attempting to delete job directory via FireCrest for Job ID: {jobInfo.Id}");
            var token = GetAuthToken(connectorClient);

            string systemName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;
            string remotePathToDelete = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}".Replace("\\", "/");
            
            var endpoint =
                $"{_firecrestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(remotePathToDelete)}";

            _log.Info($"Sending DELETE request to endpoint: {endpoint}");

            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _log.Warn(
                    $"Failed to delete job directory for Job ID {jobInfo.Id}. Status: {response.StatusCode}. Response: {errorContent}");
            }
            else
            {
                _log.Info(
                    $"Successfully received response for job directory deletion for Job ID {jobInfo.Id}. Status: {response.StatusCode}.");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.Error($"An exception occurred while deleting job directory for Job ID {jobInfo.Id}: {ex.Message}", ex);
            return false;
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
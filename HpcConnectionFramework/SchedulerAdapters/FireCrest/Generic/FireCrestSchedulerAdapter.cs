using System;
using System.Diagnostics;
using System.Security.Cryptography;
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
using HEAppE.Services.FirecRest;
using HEAppE.Utils;



namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

public class FirecRestSchedulerAdapter : ISchedulerAdapter
{
    #region Instances

    protected ISchedulerDataConvertor _convertor;
    protected ICommands _commands;
    protected ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly IFirecRestTokenService _tokenService;
    protected HttpClient _httpClient => _httpClientFactory.CreateClient("FirecREST");

    protected string _firecRestUrl;
    protected string _firecRestIdpUrl;

    public string FirecRestUrl { private get => _firecRestUrl; set => _firecRestUrl = value.TrimEnd('/'); }
    public string FirecRestIdpUrl { private get => _firecRestIdpUrl; set => _firecRestIdpUrl = value.TrimEnd('/'); }

    public string ClientId { private get; set; }
    public string ClientSecret { private get; set; }
    public string ClusterName { get; set; }
    public Dictionary<string, string> CustomConfiguration { get; set; }

    protected static readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    #endregion

    #region Constructors

    public FirecRestSchedulerAdapter(ISchedulerDataConvertor convertor, IHttpClientFactory httpClientFactory, IFirecRestTokenService tokenService, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _commands = new FirecRestCommands();
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
    }

    #endregion

    #region Private Methods

    private async Task<string> GetAuthTokenAsync()
    {
        try
        {
            return await _tokenService.GetTokenAsync(ClientId, ClientSecret, FirecRestIdpUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to retrieve FirecRest authentication token: {ex.Message}");
            throw new SshCommandException("Failed to retrieve FirecRest authentication token", ex.Message);
        }
    }

    /// <summary>
    /// Expands a path starting with '~' to the absolute home directory path
    /// by querying the Firecrest stat endpoint. Firecrest health checkers only
    /// recognise absolute mount-point paths (e.g. /users/..., /scratch/...),
    /// so tilde notation must be resolved before any filesystem API call.
    /// The resolved path is cached per (clusterName, token) within a request.
    /// </summary>
    private string ExpandRemotePath(string path, string username)
    {
        string homeDirTemplate = "/users/{username}";
        if (CustomConfiguration != null && CustomConfiguration.TryGetValue("HomeDirectoryTemplate", out var template))
        {
            homeDirTemplate = template;
        }

        var homeDir = homeDirTemplate
            .Replace("{username}", username)
            .Replace("{USER}", username)
            .Replace("$USER", username);

        // 1. Expand ~ if it starts with ~
        var result = path;
        if (result.StartsWith("~"))
        {
            result = homeDir + result.Substring(1);
        }

        // 2. Expand $USER, ${USER}, $HOME
        result = result
            .Replace("$USER", username)
            .Replace("${USER}", username)
            .Replace("$HOME", homeDir)
            .Replace("${HOME}", homeDir);

        _logger.LogInformation($"[ExpandRemotePath] Expanded '{path}' → '{result}'");
        return result;
    }

    private async Task CreateDirectoryAsync(string endpoint, string token, string directoryPath, object requestBody)
    {

        var jsonContent = JsonSerializer.Serialize(requestBody);

        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

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
                        await CreateDirectoryAsync(endpoint, token, parentDirectory, updatedRequestBody);
                        _logger.LogDebug($"[CreateDirectory] RECURSION DONE. Parent {parentDirectory} handled.");

                        _logger.LogDebug($"[CreateDirectory] RETRYING original: {directoryPath}");
                        using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        retryRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var retryResponse = await _httpClient.SendAsync(retryRequest);
                        var retryContent = await retryResponse.Content.ReadAsStringAsync();

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

    private async Task<string> CloneOrUpdateRepositoryLocallyAsync(string repoUrl, string branch)
    {
        var urlBytes = Encoding.UTF8.GetBytes(repoUrl);
        var urlHash = string.Concat(SHA256.HashData(urlBytes).Select(b => b.ToString("x2")));
        var tempCacheDir = Path.Combine(Path.GetTempPath(), "heappe_scripts_cache_" + urlHash);

        _logger.LogInformation($"Local cache directory for git repository: {tempCacheDir}");
        Directory.CreateDirectory(tempCacheDir);

        string gitDir = Path.Combine(tempCacheDir, ".git");
        if (!Directory.Exists(gitDir))
        {
            if (Directory.EnumerateFileSystemEntries(tempCacheDir).Any())
            {
                Directory.Delete(tempCacheDir, true);
                Directory.CreateDirectory(tempCacheDir);
            }

            _logger.LogInformation($"Cloning repository {repoUrl} (branch: {branch}) locally on the server...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --single-branch -b {branch} \"{repoUrl}\" .",
                WorkingDirectory = tempCacheDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start git clone process.");
            }
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Failed to clone git repository on HEAppE server: {error}");
            }
        }
        else
        {
            _logger.LogInformation($"Pulling latest changes for branch {branch} in local cache...");
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"pull origin {branch}",
                WorkingDirectory = tempCacheDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start git pull process.");
            }
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogWarning($"Failed to pull git repository: {error}. Cleaning directory and re-cloning.");
                Directory.Delete(tempCacheDir, true);
                return await CloneOrUpdateRepositoryLocallyAsync(repoUrl, branch);
            }
        }

        return tempCacheDir;
    }

    private string FindKeyScriptsDirectory(string rootPath)
    {
        var searchPath = Path.Combine(rootPath, "HPC", ".key_scripts");
        if (Directory.Exists(searchPath))
        {
            return searchPath;
        }

        searchPath = Path.Combine(rootPath, ".key_scripts");
        if (Directory.Exists(searchPath))
        {
            return searchPath;
        }

        var directories = Directory.GetDirectories(rootPath, ".key_scripts", SearchOption.AllDirectories);
        if (directories.Length > 0)
        {
            return directories[0];
        }

        return null;
    }

    private async Task UploadFileAsync(string firecrestUrl, string token, string clusterName, string remoteDirectoryPath, string fileName, byte[] fileContent)
    {
        var endpoint = $"{firecrestUrl}/filesystem/{clusterName}/ops/upload?path={Uri.EscapeDataString(remoteDirectoryPath)}";
        _logger.LogDebug($"[Firecrest Upload] POST {endpoint} for file {fileName}");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        var fileContentContent = new ByteArrayContent(fileContent);
        fileContentContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContentContent, "file", fileName);

        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new FirecrestApiException($"Failed to upload file {fileName} to {remoteDirectoryPath}. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }
    }

    private async Task ChmodAsync(string firecrestUrl, string token, string clusterName, string remoteFilePath, string mode)
    {
        var endpoint = $"{firecrestUrl}/filesystem/{clusterName}/ops/chmod";
        _logger.LogDebug($"[Firecrest Chmod] PUT {endpoint} for file {remoteFilePath}");

        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var chmodPayload = new
        {
            sourcePath = remoteFilePath,
            mode = mode
        };
        var jsonContent = JsonSerializer.Serialize(chmodPayload);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new FirecrestApiException($"Failed to change permissions for {remoteFilePath}. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }
    }

    #endregion

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        try
        {
            _logger.LogDebug($"[SubmitJob] STARTING... JobId: {jobSpecification.Id}, Name: {jobSpecification.Name}");
            var token = await GetAuthTokenAsync();

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

                    var submitResponse = await _httpClient.SendAsync(submitRequest);

                    var submitResponseContent = await submitResponse.Content.ReadAsStringAsync();

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
                var queriedTasks = await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, tasksToQuery, null);
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

    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string key)
    {
        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        // if called by some service, just echo already existing tasks state for now
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
        {
            return submittedTasksInfo;
        }

        var token = await GetAuthTokenAsync();

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

        return submittedTasksInfo;
    }

    public async Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submittedTasksInfo, string message)
    {
        if (submittedTasksInfo == null || !submittedTasksInfo.Any())
        {
            throw new ArgumentException("Cannot cancel jobs: The provided list of tasks is null or empty.",
                nameof(submittedTasksInfo));
        }

        try
        {
            var token = await GetAuthTokenAsync();
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
                await Task.WhenAll(cancellationTasks);
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

    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        try
        {
            var token = await GetAuthTokenAsync();

            string clusterName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            string jobDirectoryPath = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");
            jobDirectoryPath = ExpandRemotePath(jobDirectoryPath, account);

            var endpoint = $"{FirecRestUrl}/filesystem/{clusterName}/ops/mkdir";
            var jobRequestBody = new { path = jobDirectoryPath, p = true };

            await CreateDirectoryAsync(endpoint, token, jobDirectoryPath, jobRequestBody);

            _logger.LogDebug($"[CreateJobDirectory] Found {jobInfo.Tasks.Count} tasks. Creating subdirectories...");

            foreach (var task in jobInfo.Tasks)
            {
                string taskDirectoryPath = $"{jobDirectoryPath}/{task.Specification.Id}".Replace("\\", "/");
                var taskRequestBody = new { path = taskDirectoryPath, p = true };
                await CreateDirectoryAsync(endpoint, token, taskDirectoryPath, taskRequestBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating job directory: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        try
        {
            var token = await GetAuthTokenAsync();

            string systemName = jobInfo.Specification.Cluster.Name;
            string account = jobInfo.Specification.ClusterUser.Username;

            string remotePathToDelete = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");

            var endpoint =
                $"{FirecRestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(remotePathToDelete)}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
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

    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType)
    {
        await Task.Delay(1);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetAllocatedNodesAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        await Task.Delay(1);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath) =>
        await _commands.GetParametersFromGenericUserScriptAsync(connectorClient, userScriptPath);

    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo) =>
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);

    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString) =>
        await _commands.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);

    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path) =>
        await _commands.CopyJobDataToTempAsync(connectorClient, jobInfo, localBasePath, hash, path);

    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash) =>
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);

    public Task CreateTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort) =>
        throw new NotSupportedException();

    public Task RemoveTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo) =>
        throw new NotSupportedException();

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost) =>
        throw new NotSupportedException();

    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath,
        string account, bool isServiceAccount)
    {
        if (isServiceAccount) return true;

        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
        {
            _logger.LogWarning("Firecrest client credentials not set. Bypassing script initialization.");
            return true;
        }

        try
        {
            var repoUrl = _scripts.ClusterScriptsRepository;
            var branch = _scripts.ClusterScriptsRepositoryBranch;
            if (string.IsNullOrEmpty(repoUrl))
            {
                _logger.LogWarning("ClusterScriptsRepository is not configured.");
                return false;
            }

            // 1. Clone or pull repo locally on HEAppE server
            var localRepoPath = await CloneOrUpdateRepositoryLocallyAsync(repoUrl, branch);

            // 2. Locate .key_scripts folder
            var localKeyScriptsPath = FindKeyScriptsDirectory(localRepoPath);
            if (localKeyScriptsPath == null)
            {
                _logger.LogError($".key_scripts directory not found in the cloned repository: {localRepoPath}");
                return false;
            }

            // 3. Obtain Firecrest URL and token
            var token = await GetAuthTokenAsync();
            var firecrestUrl = FirecRestUrl;
            var clusterName = ClusterName ?? "Unknown";

            // 4. Construct remote destination directory
            var rootDir = Path.Combine(_scripts.ScriptsBasePath, $".{clusterProjectRootDirectory}").Replace('\\', '/');
            rootDir = ExpandRemotePath(rootDir, account);
            var targetDir = $"{rootDir}/.key_scripts";

            // 5. Create remote directory
            var mkdirEndpoint = $"{firecrestUrl}/filesystem/{clusterName}/ops/mkdir";
            var mkdirRequestBody = new { path = targetDir, p = true };
            await CreateDirectoryAsync(mkdirEndpoint, token, targetDir, mkdirRequestBody);

            // 6. Calculate placeholder replacement
            var sedReplacement = $"{localBasepath}/{_scripts.InstanceIdentifierPath}/{_scripts.SubExecutionsPath}/{account}";

            // 7. For each file in the local .key_scripts directory:
            var files = Directory.GetFiles(localKeyScriptsPath);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                byte[] fileContent;

                if (fileName == "remote-cmd3.sh")
                {
                    var fileText = await File.ReadAllTextAsync(filePath);
                    fileText = fileText.Replace("TODO", sedReplacement);
                    fileContent = Encoding.UTF8.GetBytes(fileText);
                }
                else
                {
                    fileContent = await File.ReadAllBytesAsync(filePath);
                }

                var remoteFilePath = $"{targetDir}/{fileName}";

                // Upload file
                await UploadFileAsync(firecrestUrl, token, clusterName, targetDir, fileName, fileContent);

                // Set file permission to executable (chmod 755)
                await ChmodAsync(firecrestUrl, token, clusterName, remoteFilePath, "755");
            }

            _logger.LogInformation($"Successfully initialized scripts directory for cluster {clusterName} via Firecrest.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize cluster scripts directory for cluster {ClusterName ?? "Unknown"}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode) =>
        await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);

    public Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog) =>
        throw new NotSupportedException();

    public Task<DryRunJobInfo> DryRunJobAsync(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification) =>
        throw new NotSupportedException();

    public Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(object schedulerConnectionConnection, List<SubmittedTaskInfo> missingTasks, ClusterAuthenticationCredentials account) =>
        throw new NotSupportedException();
}

// fortress of lies
class FirecRestCommands : ICommands
{
    public string InterpreterCommand => "";

    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
    {
        await Task.Delay(1);
    }

    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        await Task.Delay(1);
    }

    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
    {
        await Task.Delay(1);
    }

    public async Task<bool> CopyJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        await Task.Delay(1);
        return true;
    }

    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
    {
        await Task.Delay(1);
    }

    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        await Task.Delay(1);
        return true;
    }

    public async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath)
    {
        await Task.Delay(1);
        return [];
    }

    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection, string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
    {
        await Task.Delay(1);
        return true;
    }

    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        await Task.Delay(1);
    }
}
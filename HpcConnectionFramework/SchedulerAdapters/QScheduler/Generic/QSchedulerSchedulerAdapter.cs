using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;

/// <summary>
///     QScheduler scheduler adapter
/// </summary>
internal class QSchedulerSchedulerAdapter : ISchedulerAdapter
{
    #region Constructors

    public QSchedulerSchedulerAdapter(ISchedulerDataConvertor convertor, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _httpClientFactory = httpClientFactory;
        _sshTunnelUtil = new SshTunnelUtils();
        _commands = new LinuxCommands(logger);
    }

    #endregion

    #region Instances

    protected ISchedulerDataConvertor _convertor;
    protected ICommands _commands;
    protected ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected static SshTunnelUtils _sshTunnelUtil;

    /// <summary>
    /// In-memory cache of the last successfully synchronized project limit (projectName → limitMs).
    /// Used to skip redundant PATCH calls when the allocation has not changed since last submit.
    /// Cache is invalidated automatically when the limit changes.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _projectLimitCache
        = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();

    #endregion

    #region Private Methods

    private int GetQSchedulerPort(Cluster cluster)
    {
        if (cluster.CustomConfiguration != null && cluster.CustomConfiguration.TryGetValue("QSchedulerPort", out var portStr) && int.TryParse(portStr, out int port))
        {
            return port;
        }
        return 3000; // default QScheduler port
    }

    private string GetQSchedulerHost(Cluster cluster)
    {
        if (cluster.CustomConfiguration != null && cluster.CustomConfiguration.TryGetValue("QSchedulerHost", out var host))
        {
            return host;
        }
        return "localhost";
    }

    /// <summary>
    /// Sanitizes a path for use as a curl argument by wrapping it in single quotes
    /// and escaping any single quotes within the path itself.
    /// This prevents shell injection through filenames.
    /// </summary>
    private static string ShellQuotePath(string path)
    {
        if (path.StartsWith("~/"))
        {
            return "$HOME/" + "'" + path.Substring(2).Replace("'", "'\\''") + "'";
        }
        if (path.StartsWith("~"))
        {
            return "$HOME" + "'" + path.Substring(1).Replace("'", "'\\''") + "'";
        }
        // Escape any existing single quotes, then wrap the whole path in single quotes.
        return "'" + path.Replace("'", "'\\''") + "'";
    }

    /// <summary>
    /// Unified helper to execute a request against the QScheduler REST API
    /// supporting both direct HTTP/HTTPS and SSH-tunneled curl requests.
    /// </summary>
    private async Task<string> ExecuteRequestAsync(
        object connectorClient, 
        Cluster cluster, 
        string method, 
        string relativeUrl, 
        byte[] payloadBytes = null,
        System.IO.Stream payloadStream = null,
        string payloadFilePath = null)
    {
        if (connectorClient is ConnectionPool.HttpConnection httpConn)
        {
            var baseUri = httpConn.BaseUri;
            var url = $"{baseUri.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
            _logger.LogInformation($"Executing direct HTTP/HTTPS request: {method} {url}");
            
            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            
            if (payloadBytes != null)
            {
                request.Content = new ByteArrayContent(payloadBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }
            else if (payloadStream != null)
            {
                request.Content = new StreamContent(payloadStream);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }
            else if (!string.IsNullOrEmpty(payloadFilePath))
            {
                byte[] localBytes = System.IO.File.ReadAllBytes(payloadFilePath);
                request.Content = new ByteArrayContent(localBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            }
            
            string content;
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
                content = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new UnableToCreateConnectionException($"QScheduler service is unreachable. Connection refused to {url}. Please verify the QScheduler service is running.", ex);
            }
            
            using (response)
            {
                // Handle project creation Conflict (409) gracefully as success
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict && 
                    method.Equals("POST", StringComparison.OrdinalIgnoreCase) && 
                    relativeUrl.StartsWith("projects", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Project already exists in QScheduler (409 Conflict): {content}");
                    return "CONFLICT: " + content;
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new QSchedulerApiException($"QScheduler direct API request failed with status {response.StatusCode}. Details: {content}", response.StatusCode, content);
                }
                return content;
            }
        }
        else
        {
            var port = GetQSchedulerPort(cluster);
            string methodArg = "";
            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                methodArg = $"-X {method.ToUpper()} ";
            }
            
            string dataArg = "";
            if (payloadBytes != null)
            {
                var jsonStr = Encoding.UTF8.GetString(payloadBytes);
                var escapedJson = jsonStr.Replace("'", "'\\''");
                dataArg = $"-H \"Content-Type: application/json\" -d '{escapedJson}' ";
            }
            else if (payloadStream != null)
            {
                using var ms = new System.IO.MemoryStream();
                payloadStream.CopyTo(ms);
                var jsonStr = Encoding.UTF8.GetString(ms.ToArray());
                var escapedJson = jsonStr.Replace("'", "'\\''");
                dataArg = $"-H \"Content-Type: application/json\" -d '{escapedJson}' ";
            }
            else if (!string.IsNullOrEmpty(payloadFilePath))
            {
                var quotedPath = ShellQuotePath(payloadFilePath);
                dataArg = $"-H \"Content-Type: application/octet-stream\" --data-binary @{quotedPath} ";
            }
            
            var host = GetQSchedulerHost(cluster);
            var cmd = $"curl -s -w \"\\nHTTP_STATUS:%{{http_code}}\" {methodArg}{dataArg}\"http://{host}:{port}/{relativeUrl.TrimStart('/')}\"";
            _logger.LogInformation($"Querying QScheduler via SSH command: \"{cmd}\"");
            
            SshCommandWrapper commandResult;
            try
            {
                commandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, cmd, _logger);
            }
            catch (SshCommandException ex)
            {
                if (ex.Contains("^7$"))
                {
                    throw new UnableToCreateConnectionException($"QScheduler service is unreachable. Connection refused to http://{host}:{port}. Please verify the QScheduler service is running and accessible from the SSH node.", ex);
                }
                throw;
            }
            
            var result = commandResult.Result;
            var statusCode = 200;
            if (result != null && result.Contains("HTTP_STATUS:"))
            {
                var idx = result.LastIndexOf("HTTP_STATUS:");
                var codeStr = result.Substring(idx + "HTTP_STATUS:".Length).Trim();
                int.TryParse(codeStr, out statusCode);
                result = result.Substring(0, idx).TrimEnd('\r', '\n');
            }
            
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && 
                relativeUrl.StartsWith("projects", StringComparison.OrdinalIgnoreCase))
            {
                if (statusCode == 409 || (result != null && (result.Contains("Conflict") || result.Contains("already exists"))))
                {
                    _logger.LogWarning($"Project already exists in QScheduler SSH mode (409 Conflict): {result}");
                    return "CONFLICT: " + result;
                }
            }
            
            if (statusCode < 200 || statusCode >= 300)
            {
                throw new QSchedulerApiException($"QScheduler SSH API request failed with status {statusCode}. Details: {result}", (HttpStatusCode)statusCode, result);
            }
            
            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(commandResult.Error))
            {
                throw new QSchedulerApiException($"QScheduler SSH API command failed. Error: {commandResult.Error}", HttpStatusCode.InternalServerError, commandResult.Error);
            }
            
            return result;
        }
    }

    #endregion

    #region ISchedulerAdapter Members

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        _logger.LogInformation($"SubmitJobAsync started for QScheduler. Cluster ID: {jobSpecification.Cluster?.Id}");
        
        var projectName = jobSpecification.Project?.AccountingString;
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = jobSpecification.Project?.Name;
        }

        // 1. Dynamic Project Registration
        if (jobSpecification.Project != null)
        {
            
            if (jobSpecification.Project.UsageType != HEAppE.DomainObjects.JobReporting.Enums.UsageType.QPUSeconds)
            {
                throw new ArgumentException($"Cannot submit job. Project '{jobSpecification.Project.Name}' does not have UsageType configured as QPUSeconds.");
            }

            var usageTypeName = jobSpecification.Project.UsageType.ToString(); // "QPUSeconds"
            var aggregations = jobSpecification.Project.ProjectClusterNodeTypeAggregations?
                .Where(a => a.ClusterNodeTypeAggregation != null)
                .ToList();
            
            if (aggregations == null || !aggregations.Any())
            {
                throw new ArgumentException($"Cannot submit job. Resource allocation limit for '{usageTypeName}' node type is not configured for project '{jobSpecification.Project.Name}'.");
            }
            
            var totalAllocation = aggregations.Sum(a => a.AllocationAmount);
            var limitMs = totalAllocation * 1000;
            _logger.LogInformation($"Registering/ensuring project '{projectName}' with limit {limitMs} ms (converted from {totalAllocation} {usageTypeName} summed from {aggregations.Count} aggregation(s)).");
            
            var projectPayload = $"{{\"name\":\"{projectName}\",\"limit_ms\":{limitMs},\"active\":true}}";
            var response = await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "POST", "projects", Encoding.UTF8.GetBytes(projectPayload));
            
            if (response != null && response.StartsWith("CONFLICT:", StringComparison.OrdinalIgnoreCase))
            {
                // Project already exists. Only send PATCH if the limit has changed since the last
                // known synchronization — avoids one unnecessary HTTP round-trip per submission.
                var cacheKey = $"{jobSpecification.Cluster.Id}:{projectName}";
                if (!_projectLimitCache.TryGetValue(cacheKey, out var cachedLimitMs) || cachedLimitMs != limitMs)
                {
                    _logger.LogInformation($"Project '{projectName}' limit changed ({cachedLimitMs} → {limitMs} ms) or not yet cached. Sending PATCH to update.");
                    var patchPayload = $"{{\"limit_ms\":{limitMs},\"active\":true}}";
                    try
                    {
                        await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "PATCH", $"projects/{projectName}", Encoding.UTF8.GetBytes(patchPayload));
                        _projectLimitCache[cacheKey] = limitMs;
                    }
                    catch (QSchedulerApiException ex) when (ex.StatusCode == HttpStatusCode.MethodNotAllowed || ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning($"QScheduler does not support project PATCH (status {ex.StatusCode}). Skipping.");
                    }
                }
                else
                {
                    _logger.LogDebug($"Project '{projectName}' limit unchanged ({limitMs} ms). Skipping redundant PATCH.");
                }
            }
            else
            {
                // New project created successfully — seed the cache.
                var cacheKey = $"{jobSpecification.Cluster.Id}:{projectName}";
                _projectLimitCache[cacheKey] = limitMs;
            }
        }

        // 2. Task Submission
        var clusterConfig = ClusterRuntimeConfiguration.For(jobSpecification.Cluster.CustomConfiguration);

        // Group tasks by their target machine (ClusterNodeType.Queue is used as machine_id).
        var taskGroups = jobSpecification.Tasks
            .GroupBy(t => t.ClusterNodeType.Queue)
            .ToList();

        _logger.LogInformation($"SubmitJobAsync: {jobSpecification.Tasks.Count} task(s) grouped into {taskGroups.Count} machine group(s).");

        // Determine if we should use sessions
        bool useSessions = false;
        if (jobSpecification.Cluster.CustomConfiguration != null &&
            jobSpecification.Cluster.CustomConfiguration.TryGetValue("QSchedulerUseSessions", out var useSessionsStr) &&
            useSessionsStr == "true")
        {
            useSessions = true;
        }

        var firstTask = jobSpecification.Tasks.FirstOrDefault();
        long? existingSessionId = null;
        if (firstTask != null && firstTask.EnvironmentVariables != null)
        {
            var envVar = firstTask.EnvironmentVariables.FirstOrDefault(e => e.Name == "HEAPPE_QSCHEDULER_USE_SESSIONS");
            if (envVar != null)
            {
                useSessions = (envVar.Value == "true");
            }

            var sessionIdVar = firstTask.EnvironmentVariables.FirstOrDefault(e => e.Name == "HEAPPE_QSCHEDULER_SESSION_ID");
            if (sessionIdVar != null && long.TryParse(sessionIdVar.Value, out var sid))
            {
                existingSessionId = sid;
                useSessions = true;
            }
        }

        var submittedTasks = new List<SubmittedTaskInfo>();

        foreach (var group in taskGroups)
        {
            var machineId = group.Key;

            if (useSessions)
            {
                long sessionId;
                if (existingSessionId.HasValue)
                {
                    sessionId = existingSessionId.Value;
                    _logger.LogInformation($"Using existing QScheduler session ID: {sessionId}");
                }
                else
                {
                    // Walltime limit for the session must cover the longest task in this group,
                    // so the session stays open until all tasks in it finish.
                    var walltimeSecs = group.Max(t => Convert.ToInt32(t.WalltimeLimit));
                    if (walltimeSecs <= 0) walltimeSecs = 3600; // default 1 hour

                    _logger.LogInformation($"Creating QScheduler session for machine ID: {machineId}, walltime: {walltimeSecs}s");

                    // Create session for this machine group
                    var walltimeMs = walltimeSecs * 1000;
                    var relativeUrl = $"sessions?machine={machineId}&project={projectName}&time_limit_ms={walltimeMs}";
                    var sessionResponse = await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "POST", relativeUrl);
                    var sessionIdStr = sessionResponse.Trim();
                    if (!long.TryParse(sessionIdStr, out sessionId))
                    {
                        _logger.LogError($"Failed to parse sessionId for machine {machineId}. Response: '{sessionResponse}'");
                        throw new Exception($"Failed to create QScheduler session for machine {machineId}. Response: {sessionResponse}");
                    }
                    _logger.LogInformation($"QScheduler session created successfully for machine {machineId}. Session ID: {sessionId}");
                }

                bool isSessionOpen = false;
                try
                {
                    _logger.LogInformation($"Querying state of session {sessionId} before task submission.");
                    var sessionResponse = await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "GET", $"sessions/{sessionId}");
                    var sessionState = sessionResponse.Trim().Replace("\"", "").ToLower();
                    _logger.LogInformation($"Session {sessionId} state: '{sessionState}'");
                    if (sessionState == "open" || sessionState == "running")
                    {
                        isSessionOpen = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to query state of session {sessionId}: {ex.Message}");
                }

                foreach (var taskSpec in group)
                {
                    var taskInfo = new SubmittedTaskInfo
                    {
                        Id = taskSpec.Id,
                        Name = taskSpec.Id.ToString(),
                        ScheduledJobId = $"session:{sessionId}",
                        State = TaskState.Submitted,
                        Specification = taskSpec
                    };

                    if (isSessionOpen)
                    {
                        System.IO.Stream payloadStream = null;
                        var payloads = HEAppE.Utils.QSchedulerPayloadContext.Payloads;
                        if (payloads != null && payloads.TryGetValue(taskSpec.Name, out var stream))
                        {
                            payloadStream = stream;
                        }

                        string payloadPath = null;
                        if (payloadStream == null)
                        {
                            var taskDir = FileSystemUtils.GetTaskClusterDirectoryPath(taskSpec, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath).Replace('\\', '/');
                            payloadPath = string.IsNullOrEmpty(taskSpec.StandardInputFile)
                                ? $"{taskDir}/payload.json"
                                : $"{taskDir}/{taskSpec.StandardInputFile}";
                        }

                        var username = credentials?.Username ?? jobSpecification.Submitter?.Username ?? "heappe";
                        var relativeUrl = $"tasks?machine={machineId}&session_id={sessionId}&user={username}";
                        _logger.LogInformation($"Session {sessionId} is open. Submitting task {taskSpec.Id} immediately.");

                        try
                        {
                            var taskResponse = await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "POST", relativeUrl, payloadStream: payloadStream, payloadFilePath: payloadPath);
                            var taskIdStr = taskResponse.Trim();
                            if (long.TryParse(taskIdStr, out long taskId))
                            {
                                taskInfo.ScheduledJobId = $"session:{sessionId}:task:{taskId}";
                                _logger.LogInformation($"QScheduler task {taskSpec.Id} submitted successfully to session {sessionId}. QScheduler Task ID: {taskId}");
                            }
                            else
                            {
                                _logger.LogError($"Failed to parse QScheduler task ID for task {taskSpec.Id}. Response: '{taskResponse}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to submit task {taskSpec.Id} immediately to session {sessionId}");
                        }
                    }

                    submittedTasks.Add(taskInfo);
                }
            }
            else
            {
                // Submit each task in this group directly to the machine (no session)
                foreach (var taskSpec in group)
                {
                    System.IO.Stream payloadStream = null;
                    var payloads = HEAppE.Utils.QSchedulerPayloadContext.Payloads;
                    if (payloads != null && payloads.TryGetValue(taskSpec.Name, out var stream))
                    {
                        payloadStream = stream;
                    }

                    string payloadPath = null;
                    if (payloadStream == null)
                    {
                        var taskDir = FileSystemUtils.GetTaskClusterDirectoryPath(taskSpec, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath).Replace('\\', '/');
                        payloadPath = string.IsNullOrEmpty(taskSpec.StandardInputFile)
                            ? $"{taskDir}/payload.json"
                            : $"{taskDir}/{taskSpec.StandardInputFile}";
                    }

                    var username = credentials?.Username ?? jobSpecification.Submitter?.Username ?? "heappe";
                    var relativeUrl = $"tasks?machine={machineId}&project={projectName}&user={username}";
                    var taskResponse = await ExecuteRequestAsync(connectorClient, jobSpecification.Cluster, "POST", relativeUrl, payloadStream: payloadStream, payloadFilePath: payloadPath);
                    var taskIdStr = taskResponse.Trim();
                    if (!long.TryParse(taskIdStr, out long taskId))
                    {
                        _logger.LogError($"Failed to parse QScheduler task ID for task {taskSpec.Id}. Response: '{taskResponse}'");
                        throw new Exception($"Failed to submit QScheduler task {taskSpec.Id}. Response: {taskResponse}");
                    }

                    _logger.LogInformation($"QScheduler task {taskSpec.Id} submitted successfully. Assigned QScheduler Task ID: {taskId}, Machine: {machineId}");
                    submittedTasks.Add(new SubmittedTaskInfo
                    {
                        Id = taskSpec.Id,
                        Name = taskSpec.Id.ToString(),
                        ScheduledJobId = $"task:{taskId}",
                        State = TaskState.Submitted,
                        Specification = taskSpec
                    });
                }
            }
        }

        _logger.LogInformation($"SubmitJobAsync finished for QScheduler. Total tasks submitted: {submittedTasks.Count}");
        return submittedTasks;
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        _logger.LogInformation("GetActualTasksInfoAsync started for QScheduler.");
        var results = new List<SubmittedTaskInfo>();
        var clusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);

        foreach (var taskInfo in submitedTasksInfo)
        {
            if (string.IsNullOrEmpty(taskInfo.ScheduledJobId))
            {
                _logger.LogWarning($"Task Info with ID {taskInfo.Id} does not have a ScheduledJobId.");
                results.Add(taskInfo);
                continue;
            }

            if (taskInfo.ScheduledJobId.StartsWith("session:") && !taskInfo.ScheduledJobId.Contains(":task:"))
            {
                var sessionId = taskInfo.ScheduledJobId.Substring("session:".Length);
                
                bool callbackEnabled = clusterConfig.Scripts.UseCallbackForHpcJobs;

                if (callbackEnabled && !taskInfo.ForceSessionSubmit && !taskInfo.ForceStatusQuery)
                {
                    _logger.LogDebug($"Callback is configured. Bypassing active polling for session {sessionId}.");
                    results.Add(taskInfo);
                    continue;
                }

                _logger.LogInformation($"Checking status of session {sessionId}.");
                
                try
                {
                    var sessionResponse = await ExecuteRequestAsync(connectorClient, cluster, "GET", $"sessions/{sessionId}");
                    var sessionState = sessionResponse.Trim().Replace("\"", "").ToLower();
                    _logger.LogInformation($"Session {sessionId} state: '{sessionState}'");

                    if (sessionState == "open" || sessionState == "running")
                    {
                        // Session is active! Now submit the task payload to this session!
                        var machineId = taskInfo.Specification.ClusterNodeType.Queue;
                        var taskDir = FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath).Replace('\\', '/');
                        var payloadPath = string.IsNullOrEmpty(taskInfo.Specification.StandardInputFile)
                            ? $"{taskDir}/payload.json"
                            : $"{taskDir}/{taskInfo.Specification.StandardInputFile}";

                        var username = taskInfo.Specification?.JobSpecification?.ClusterUser?.Username ?? 
                                       taskInfo.Specification?.JobSpecification?.Submitter?.Username ?? 
                                       "heappe";
                        var relativeUrl = $"tasks?machine={machineId}&session_id={sessionId}&user={username}";
                        _logger.LogInformation($"Submitting task {taskInfo.Id} to session {sessionId}.");
                        
                        var taskResponse = await ExecuteRequestAsync(connectorClient, cluster, "POST", relativeUrl, payloadFilePath: payloadPath);
                        var taskIdStr = taskResponse.Trim();
                        if (!long.TryParse(taskIdStr, out long taskId))
                        {
                            _logger.LogError($"Failed to parse QScheduler task ID for task {taskInfo.Id}. Response: '{taskResponse}'");
                            throw new Exception($"Failed to submit QScheduler task {taskInfo.Id}. Response: {taskResponse}");
                        }

                        _logger.LogInformation($"QScheduler task {taskInfo.Id} submitted successfully to session {sessionId}. QScheduler Task ID: {taskId}");
                        
                        // Transition ScheduledJobId to include session ID and task ID
                        taskInfo.ScheduledJobId = $"session:{sessionId}:task:{taskId}";
                        taskInfo.State = TaskState.Submitted;
                    }
                    else if (sessionState == "closed")
                    {
                        _logger.LogError($"Session {sessionId} closed before task could be submitted.");
                        taskInfo.State = TaskState.Failed;
                        taskInfo.ErrorMessage = "Session closed before task could be submitted.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to query QScheduler session state for {sessionId}");
                }
                results.Add(taskInfo);
            }
            else // Starts with "task:" or "session:...:task:" or plain taskId (direct mode)
            {
                string taskId;
                if (taskInfo.ScheduledJobId.StartsWith("session:") && taskInfo.ScheduledJobId.Contains(":task:"))
                {
                    taskId = taskInfo.ScheduledJobId.Split(new[] { ":task:" }, StringSplitOptions.None)[1];
                }
                else
                {
                    taskId = taskInfo.ScheduledJobId.StartsWith("task:") 
                        ? taskInfo.ScheduledJobId.Substring("task:".Length) 
                        : taskInfo.ScheduledJobId;
                }

                bool callbackEnabled = clusterConfig.Scripts.UseCallbackForHpcJobs;

                if (callbackEnabled && !taskInfo.ForceStatusQuery)
                {
                    // Bypass active SSH polling - just keep current DB state!
                    _logger.LogDebug($"Callback is configured. Bypassing active polling for task {taskId}. State remains: {taskInfo.State}");
                    results.Add(taskInfo);
                    continue;
                }

                // Call active polling
                _logger.LogInformation($"Querying QScheduler task {taskId} status.");
                try
                {
                    var taskResponse = await ExecuteRequestAsync(connectorClient, cluster, "GET", $"tasks/{taskId}");
                    _logger.LogDebug($"QScheduler status response for task {taskId}: '{taskResponse}'");
                    var parsed = _convertor.ReadParametersFromResponse(cluster, taskResponse).FirstOrDefault();
                    if (parsed != null)
                    {
                        _logger.LogInformation($"Parsed task {taskId} state: {parsed.State}, ErrorMessage: '{parsed.ErrorMessage}'");
                        taskInfo.State = parsed.State;
                        taskInfo.ErrorMessage = parsed.ErrorMessage;
                    }
                    else
                    {
                        _logger.LogWarning($"QScheduler converter returned empty results for task {taskId} response.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to query QScheduler task state for {taskId}");
                    taskInfo.State = TaskState.Failed;
                }
                results.Add(taskInfo);
            }
        }
        _logger.LogInformation("GetActualTasksInfoAsync finished for QScheduler.");
        return results;
    }

    public async Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
    {
        _logger.LogInformation($"CancelJobAsync started for QScheduler. Tasks to cancel: {submitedTasksInfo.Count()}");
        foreach (var taskInfo in submitedTasksInfo)
        {
            if (string.IsNullOrEmpty(taskInfo.ScheduledJobId))
            {
                _logger.LogWarning($"Task Info with ID {taskInfo.Id} has no ScheduledJobId. Skipping cancellation.");
                continue;
            }

            if (taskInfo.ScheduledJobId.StartsWith("session:"))
            {
                var sessionId = taskInfo.ScheduledJobId.Substring("session:".Length);
                _logger.LogInformation($"Cancelling QScheduler session {sessionId}.");
                try
                {
                    await ExecuteRequestAsync(connectorClient, taskInfo.NodeType.Cluster, "DELETE", $"sessions/{sessionId}");
                    _logger.LogInformation($"QScheduler session {sessionId} cancellation requested.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to cancel QScheduler session {sessionId}.");
                    throw;
                }
            }
            else
            {
                var taskId = taskInfo.ScheduledJobId.StartsWith("task:") 
                    ? taskInfo.ScheduledJobId.Substring("task:".Length) 
                    : taskInfo.ScheduledJobId;
                _logger.LogInformation($"Cancelling QScheduler task {taskId}.");
                try
                {
                    await ExecuteRequestAsync(connectorClient, taskInfo.NodeType.Cluster, "DELETE", $"tasks/{taskId}");
                    _logger.LogInformation($"QScheduler task {taskId} cancellation requested.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to cancel QScheduler task {taskId}.");
                    throw;
                }
            }
        }
    }

    public async Task<string> GetMachineArchitectureAsync(object connectorClient, Cluster cluster, string machineId)
    {
        _logger.LogInformation($"GetMachineArchitectureAsync started for machine ID: {machineId}, cluster ID: {cluster.Id}");
        var result = await ExecuteRequestAsync(connectorClient, cluster, "GET", $"machine/{machineId}/arch");
        _logger.LogDebug($"GetMachineArchitectureAsync response for machine ID {machineId}: '{result}'");
        _logger.LogInformation($"GetMachineArchitectureAsync completed for machine ID {machineId}. Response length: {result?.Length ?? 0} chars.");
        return result;
    }

    public async Task<string> GetMachineCalibrationAsync(object connectorClient, Cluster cluster, string machineId, string calibrationId, string endpoint)
    {
        _logger.LogInformation($"GetMachineCalibrationAsync started for machine ID: {machineId}, calibration ID: {calibrationId}, endpoint: {endpoint}, cluster ID: {cluster.Id}");
        var relativeUrl = $"machine/{machineId}/calibration/{calibrationId}/{endpoint}";
        var result = await ExecuteRequestAsync(connectorClient, cluster, "GET", relativeUrl);
        _logger.LogDebug($"GetMachineCalibrationAsync response for machine ID {machineId}: '{result}'");
        _logger.LogInformation($"GetMachineCalibrationAsync completed for machine ID {machineId}. Response length: {result?.Length ?? 0} chars.");
        return result;
    }

    public Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType)
    {
        return Task.FromResult(new ClusterNodeUsage
        {
            NodeType = nodeType,
            NodesUsed = 0,
            Priority = 0,
            TotalJobs = 0
        });
    }

    public Task<IEnumerable<string>> GetAllocatedNodesAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        return Task.FromResult<IEnumerable<string>>([]);
    }

    public Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath)
    {
        return Task.FromResult<IEnumerable<string>>([]);
    }

    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
    {
        if (connectorClient is ConnectionPool.HttpConnection) return;
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);
    }

    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        if (connectorClient is ConnectionPool.HttpConnection) return;
        await _commands.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);
    }

    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
    {
        if (connectorClient is ConnectionPool.HttpConnection)
        {
            _logger.LogInformation("QScheduler HTTP mode: skipping CreateJobDirectoryAsync");
            return;
        }
        await _commands.CreateJobDirectoryAsync(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        if (connectorClient is ConnectionPool.HttpConnection)
        {
            _logger.LogInformation("QScheduler HTTP mode: skipping DeleteJobDirectoryAsync");
            return true;
        }
        return await _commands.DeleteJobDirectoryAsync(connectorClient, jobInfo, localBasePath);
    }

    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
    {
        if (connectorClient is ConnectionPool.HttpConnection)
        {
            _logger.LogInformation("QScheduler HTTP mode: skipping CopyJobDataToTempAsync");
            return;
        }
        await _commands.CopyJobDataToTempAsync(connectorClient, jobInfo, localBasePath, hash, path);
    }

    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath)
    {
        if (connectorClient is ConnectionPool.HttpConnection)
        {
            _logger.LogInformation("QScheduler HTTP mode: skipping CopyJobDataFromTempAsync");
            return;
        }
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);
    }

    public async Task CreateTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        if (connectorClient is ConnectionPool.HttpConnection) return;
        await _sshTunnelUtil.CreateTunnelAsync(connectorClient, taskInfo.Id, nodeHost, nodePort);
    }

    public async Task RemoveTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        if (connectorClient is ConnectionPool.HttpConnection) return;
        await _sshTunnelUtil.RemoveTunnelAsync(connectorClient, taskInfo.Id);
    }

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        return _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);
    }

    public Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection, string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount, Dictionary<string, string>? customConfiguration)
    {
        _logger.LogInformation("Skipping cluster script directory initialization for QScheduler.");
        return Task.FromResult(true);
    }

    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        if (schedulerConnectionConnection is ConnectionPool.HttpConnection) return true;
        return await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
    }

    public Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog)
    {
        return Task.FromResult<dynamic>(checkLog);
    }

    public Task<DryRunJobInfo> DryRunJobAsync(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification)
    {
        return Task.FromResult(new DryRunJobInfo
        {
            Message = "Dry run simulation successful for QScheduler."
        });
    }

    /// <summary>
    /// QScheduler REST API does not expose a historical tasks endpoint.
    /// See: https://github.com/It4innovations/qscheduler — only GET /tasks/{id} for current state is available.
    /// Known limitation: historical task recovery after HEAppE restart is not possible with the current QScheduler API.
    /// </summary>
    public Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(object schedulerConnectionConnection, List<SubmittedTaskInfo> missingTasks, ClusterAuthenticationCredentials account)
    {
        _logger.LogWarning($"GetHistoricalTasksInfoAsync called for QScheduler, but QScheduler REST API does not expose a historical tasks endpoint. Returning empty list for {missingTasks.Count} missing task(s).");
        return Task.FromResult<IEnumerable<SubmittedTaskInfo>>([]);
    }

    public async Task<long> OpenSessionAsync(object connectorClient, Cluster cluster, string machineId, string project, int walltimeLimitSecs)
    {
        var walltimeMs = walltimeLimitSecs * 1000;
        var relativeUrl = $"sessions?machine={machineId}&project={project}&time_limit_ms={walltimeMs}";
        var sessionResponse = await ExecuteRequestAsync(connectorClient, cluster, "POST", relativeUrl);
        var sessionIdStr = sessionResponse.Trim();
        if (!long.TryParse(sessionIdStr, out long sessionId))
        {
            _logger.LogError($"Failed to parse sessionId for machine {machineId}. Response: '{sessionResponse}'");
            throw new Exception($"Failed to create QScheduler session for machine {machineId}. Response: {sessionResponse}");
        }
        return sessionId;
    }

    public async Task CloseSessionAsync(object connectorClient, Cluster cluster, long sessionId)
    {
        await ExecuteRequestAsync(connectorClient, cluster, "DELETE", $"sessions/{sessionId}");
    }

    #endregion
}

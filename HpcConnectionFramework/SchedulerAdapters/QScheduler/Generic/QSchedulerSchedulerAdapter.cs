using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
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

    public QSchedulerSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _sshTunnelUtil = new SshTunnelUtils();
        _commands = new LinuxCommands(logger);
    }

    #endregion

    #region Instances

    protected ISchedulerDataConvertor _convertor;
    protected ICommands _commands;
    protected ILogger _logger;
    protected static SshTunnelUtils _sshTunnelUtil;

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

    #endregion

    #region ISchedulerAdapter Members

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        _logger.LogInformation($"SubmitJobAsync started for QScheduler. Cluster ID: {jobSpecification.Cluster?.Id}");
        var port = GetQSchedulerPort(jobSpecification.Cluster);
        var clusterConfig = ClusterRuntimeConfiguration.For(jobSpecification.Cluster.CustomConfiguration);

        // Group tasks by their target machine (ClusterNodeType.Queue is used as machine_id).
        // Each group creates its own session because a QScheduler session is scoped to a single machine_id.
        var taskGroups = jobSpecification.Tasks
            .GroupBy(t => int.TryParse(t.ClusterNodeType.Queue, out int mid) ? mid : 1)
            .ToList();

        _logger.LogInformation($"SubmitJobAsync: {jobSpecification.Tasks.Count} task(s) grouped into {taskGroups.Count} machine group(s).");

        var submittedTasks = new List<SubmittedTaskInfo>();

        foreach (var group in taskGroups)
        {
            var machineId = group.Key;

            // Session logic is currently commented out as requested.
            /*
            // Walltime limit for the session must cover the longest task in this group,
            // so the session stays open until all tasks in it finish.
            var walltimeSecs = group.Max(t => Convert.ToInt32(t.WalltimeLimit));
            if (walltimeSecs <= 0) walltimeSecs = 3600; // default 1 hour

            _logger.LogInformation($"Creating QScheduler session for machine ID: {machineId}, walltime: {walltimeSecs}s");

            // 1. Create session for this machine group
            var sessionCmd = $"curl -s -X POST \"http://localhost:{port}/sessions?machine_id={machineId}&time_limit_secs={walltimeSecs}\"";
            _logger.LogInformation($"Creating QScheduler session. Command: \"{sessionCmd}\"");
            var sessionCommandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, sessionCmd, _logger);
            var sessionIdStr = sessionCommandResult.Result.Trim();
            if (!long.TryParse(sessionIdStr, out long sessionId))
            {
                _logger.LogError($"Failed to parse sessionId for machine {machineId}. Command output: '{sessionCommandResult.Result}', Command error: '{sessionCommandResult.Error}'");
                throw new Exception($"Failed to create QScheduler session for machine {machineId}. Output: {sessionCommandResult.Result}, Error: {sessionCommandResult.Error}");
            }
            _logger.LogInformation($"QScheduler session created successfully for machine {machineId}. Session ID: {sessionId}");

            // Poll session status until it transitions from "waiting" to "open"
            var sessionStatusCmd = $"curl -s http://localhost:{port}/sessions/{sessionId}";
            var sessionState = "waiting";
            int maxAttempts = 30; // 30 attempts, 1 second wait each
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _logger.LogInformation($"Checking status of session {sessionId} (Attempt {attempt}/{maxAttempts}). Command: \"{sessionStatusCmd}\"");
                var statusResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, sessionStatusCmd, _logger);
                sessionState = statusResult.Result.Trim().Replace("\"", ""); // strip quotes
                _logger.LogInformation($"Session {sessionId} status: '{sessionState}'");

                if (sessionState == "open")
                {
                    break;
                }
                if (sessionState == "closed")
                {
                    throw new Exception($"Session {sessionId} closed prematurely.");
                }

                await Task.Delay(1000); // Wait 1 second before next poll
            }

            if (sessionState != "open")
            {
                throw new Exception($"QScheduler session {sessionId} did not start within the timeout limit. Current state: {sessionState}");
            }
            */

            // 2. Submit each task in this group directly to the machine (no session)
            foreach (var taskSpec in group)
            {
                var taskDir = FileSystemUtils.GetTaskClusterDirectoryPath(taskSpec, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath).Replace('\\', '/');
                var payloadPath = string.IsNullOrEmpty(taskSpec.StandardInputFile)
                    ? $"{taskDir}/payload.json"
                    : $"{taskDir}/{taskSpec.StandardInputFile}";

                // Wrap path in single quotes to prevent shell injection through special characters in filenames.
                var quotedPayloadPath = ShellQuotePath(payloadPath);
                var submitCmd = $"curl -s -X POST --data-binary @{quotedPayloadPath} \"http://localhost:{port}/tasks?machine_id={machineId}\"";
                _logger.LogInformation($"Submitting task {taskSpec.Id} to QScheduler machine {machineId} (no session). Command: \"{submitCmd}\"");
                var taskCommandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, submitCmd, _logger);
                var taskIdStr = taskCommandResult.Result.Trim();
                if (!long.TryParse(taskIdStr, out long taskId))
                {
                    _logger.LogError($"Failed to parse QScheduler task ID for task {taskSpec.Id}. Command output: '{taskCommandResult.Result}', Command error: '{taskCommandResult.Error}'");
                    throw new Exception($"Failed to submit QScheduler task {taskSpec.Id}. Output: {taskCommandResult.Result}, Error: {taskCommandResult.Error}");
                }

                _logger.LogInformation($"QScheduler task {taskSpec.Id} submitted successfully. Assigned QScheduler Task ID: {taskId}, Machine: {machineId}");
                submittedTasks.Add(new SubmittedTaskInfo
                {
                    Id = taskSpec.Id,
                    Name = taskSpec.Id.ToString(),
                    ScheduledJobId = taskId.ToString(),
                    State = TaskState.Submitted,
                    Specification = taskSpec
                });
            }
        }

        _logger.LogInformation($"SubmitJobAsync finished for QScheduler. Total tasks submitted: {submittedTasks.Count}");
        return submittedTasks;
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        _logger.LogInformation("GetActualTasksInfoAsync started for QScheduler.");
        var port = GetQSchedulerPort(cluster);
        var results = new List<SubmittedTaskInfo>();
        foreach (var taskInfo in submitedTasksInfo)
        {
            if (string.IsNullOrEmpty(taskInfo.ScheduledJobId))
            {
                _logger.LogWarning($"Task Info with ID {taskInfo.Id} does not have a ScheduledJobId.");
                results.Add(taskInfo);
                continue;
            }
            var cmd = $"curl -s http://localhost:{port}/tasks/{taskInfo.ScheduledJobId}";
            _logger.LogInformation($"Querying QScheduler task {taskInfo.ScheduledJobId} status on port {port}. Command: \"{cmd}\"");
            try
            {
                var commandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, cmd, _logger);
                _logger.LogDebug($"QScheduler status response for task {taskInfo.ScheduledJobId}: '{commandResult.Result}'");
                var parsed = _convertor.ReadParametersFromResponse(cluster, commandResult.Result).FirstOrDefault();
                if (parsed != null)
                {
                    _logger.LogInformation($"Parsed task {taskInfo.ScheduledJobId} state: {parsed.State}, ErrorMessage: '{parsed.ErrorMessage}'");
                    taskInfo.State = parsed.State;
                    taskInfo.ErrorMessage = parsed.ErrorMessage;
                }
                else
                {
                    _logger.LogWarning($"QScheduler converter returned empty results for task {taskInfo.ScheduledJobId} response.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to query QScheduler task state for {taskInfo.ScheduledJobId}");
                taskInfo.State = TaskState.Failed;
                taskInfo.ErrorMessage = ex.Message;
            }
            results.Add(taskInfo);
        }
        _logger.LogInformation("GetActualTasksInfoAsync finished for QScheduler.");
        return results;
    }

    public Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
    {
        // QScheduler does not expose a cancel endpoint in its REST API.
        // Tasks are automatically cancelled when their parent session expires (time_limit_secs).
        // See: https://github.com/It4innovations/qscheduler
        _logger.LogError("CancelJobAsync is not supported by QScheduler REST API. Tasks will be cancelled only when their session time limit expires.");
        throw new NotSupportedException("QScheduler does not support explicit job cancellation via API. Tasks are cancelled automatically when their session expires.");
    }

    public async Task<string> GetMachineArchitectureAsync(object connectorClient, Cluster cluster, int machineId)
    {
        _logger.LogInformation($"GetMachineArchitectureAsync started for machine ID: {machineId}, cluster ID: {cluster.Id}");
        var port = GetQSchedulerPort(cluster);
        var cmd = $"curl -s http://localhost:{port}/machine/{machineId}/arch";
        _logger.LogInformation($"Querying machine architecture via command: \"{cmd}\"");
        var commandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, cmd, _logger);
        // Log at Debug level — the response may be a large JSON topology payload.
        _logger.LogDebug($"GetMachineArchitectureAsync response for machine ID {machineId}: '{commandResult.Result}'");
        _logger.LogInformation($"GetMachineArchitectureAsync completed for machine ID {machineId}. Response length: {commandResult.Result?.Length ?? 0} chars.");
        return commandResult.Result;
    }

    public async Task<string> GetMachineCalibrationAsync(object connectorClient, Cluster cluster, int machineId, string calibrationId, string endpoint)
    {
        _logger.LogInformation($"GetMachineCalibrationAsync started for machine ID: {machineId}, calibration ID: {calibrationId}, endpoint: {endpoint}, cluster ID: {cluster.Id}");
        var port = GetQSchedulerPort(cluster);
        var cmd = $"curl -s http://localhost:{port}/machine/{machineId}/calibration/{calibrationId}/{endpoint}";
        _logger.LogInformation($"Querying machine calibration via command: \"{cmd}\"");
        var commandResult = await SshCommandUtils.RunSshCommandAsync(connectorClient, cmd, _logger);
        // Log at Debug level — calibration payloads can be large (readout matrices, gate fidelities, etc.).
        _logger.LogDebug($"GetMachineCalibrationAsync response for machine ID {machineId}: '{commandResult.Result}'");
        _logger.LogInformation($"GetMachineCalibrationAsync completed for machine ID {machineId}. Response length: {commandResult.Result?.Length ?? 0} chars.");
        return commandResult.Result;
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
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);
    }

    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        await _commands.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);
    }

    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, bool sharedAccountsPoolMode)
    {
        await _commands.CreateJobDirectoryAsync(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        return await _commands.DeleteJobDirectoryAsync(connectorClient, jobInfo, localBasePath);
    }

    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
    {
        await _commands.CopyJobDataToTempAsync(connectorClient, jobInfo, localBasePath, hash, path);
    }

    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath)
    {
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);
    }

    public async Task CreateTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        await _sshTunnelUtil.CreateTunnelAsync(connectorClient, taskInfo.Id, nodeHost, nodePort);
    }

    public async Task RemoveTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        await _sshTunnelUtil.RemoveTunnelAsync(connectorClient, taskInfo.Id);
    }

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        return _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);
    }

    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection, string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount, Dictionary<string, string>? customConfiguration)
    {
        return await _commands.InitializeClusterScriptDirectoryAsync(schedulerConnectionConnection, clusterProjectRootDirectory, overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount, customConfiguration);
    }

    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
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

    #endregion
}

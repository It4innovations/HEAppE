using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;

/// <summary>
///     Slurm scheduler adapter
/// </summary>
internal class SlurmSchedulerAdapter : ISchedulerAdapter
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="convertor"></param>
    public SlurmSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _sshTunnelUtil = new SshTunnelUtils();
        _commands = new LinuxCommands(logger);
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Maximum number of jobs per single SSH status query batch.
    ///     Prevents excessively long SSH commands when monitoring many jobs simultaneously.
    /// </summary>
    private const int MaxJobStatusBatchSize = 20;

    /// <summary>
    ///     Get actual tasks (HPC jobs) information - executes in chunks to avoid
    ///     overly long SSH commands when many jobs are queried simultaneously.
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="schedulerJobIdClusterAllocationNamePairs">Scheduler job id´s pair</param>
    /// <returns></returns>
    /// <exception cref="SlurmException"></exception>
    private async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<(string ScheduledJobId, string ClusterAllocationName)> schedulerJobIdClusterAllocationNamePairs)
    {
        var allPairs = schedulerJobIdClusterAllocationNamePairs.ToList();
        _logger.LogInformation($"Getting actual tasks information for jobs: \"{string.Join(", ", allPairs.Select(s => s.ScheduledJobId))}\"");

        // Split into chunks to avoid excessively long SSH command lines
        var chunks = allPairs
            .Select((pair, index) => (pair, index))
            .GroupBy(x => x.index / MaxJobStatusBatchSize)
            .Select(g => g.Select(x => x.pair).ToList())
            .ToList();

        var allResults = new List<SubmittedTaskInfo>();

        foreach (var chunk in chunks)
        {
            var chunkResult = await ExecuteStatusBatchAsync(connectorClient, cluster, chunk);
            allResults.AddRange(chunkResult);
        }

        return allResults;
    }

    /// <summary>
    ///     Execute a single batch of scontrol status queries over one SSH command.
    /// </summary>
    private async Task<IEnumerable<SubmittedTaskInfo>> ExecuteStatusBatchAsync(object connectorClient, Cluster cluster,
        IList<(string ScheduledJobId, string ClusterAllocationName)> batch)
    {
        SshCommandWrapper command = null;
        StringBuilder cmdBuilder = new();

        foreach (var (ScheduledJobId, ClusterAllocationName) in batch)
        {
            var allocationCluster = string.Empty;
            if (!string.IsNullOrEmpty(ClusterAllocationName)) allocationCluster = $"-M {ClusterAllocationName} ";
            cmdBuilder.Append($"{_commands.InterpreterCommand} 'scontrol show JobId {allocationCluster}{ScheduledJobId} -o';");
        }

        var sshCommand = cmdBuilder.ToString();

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sshCommand, _logger);
            _logger.LogDebug($"Raw scheduler response for jobs {string.Join(", ", batch.Select(s => s.ScheduledJobId))}: {command.Result}");
            var submittedTasksInfo = _convertor.ReadParametersFromResponse(cluster, command.Result).ToList();
            _logger.LogInformation($"Successfully retrieved information for {submittedTasksInfo.Count} tasks in batch of {batch.Count}.");
            return submittedTasksInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to get actual tasks info for jobs: {string.Join(", ", batch.Select(s => s.ScheduledJobId))}. Result: {command?.Result}, Error: {command?.Error}");
            throw new SlurmException(
                "GetActualTasksInfo", ex,
                string.Join(", ", batch.Select(s => s.ScheduledJobId).ToList()),
                command?.Result ?? string.Empty,
                command?.Error ?? ex.Message)
            {
                CommandError = command?.Error ?? ex.Message
            };
        }
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Convertor reference.
    /// </summary>
    protected ISchedulerDataConvertor _convertor;

    /// <summary>
    ///     Commands
    /// </summary>
    protected ICommands _commands;

    /// <summary>
    ///     Logger
    /// </summary>
    protected ILogger _logger;

    /// <summary>
    ///     SSH tunnel
    /// </summary>
    protected static SshTunnelUtils _sshTunnelUtil;

    #endregion

    #region ISchedulerAdapter Members

    /// <summary>
    ///     Submit job to scheduler
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    /// <exception cref="SlurmException"></exception>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        var sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "sbatch");
        _logger.LogInformation($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");

        var clusterConfig = ClusterRuntimeConfiguration.For(jobSpecification.Cluster.CustomConfiguration);
        var sbatchCmd = $"{_commands.InterpreterCommand} '{clusterConfig.GetExecuteCmdScriptPath(jobSpecification.Project.AccountingString, credentials?.Username)} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";

        SshCommandWrapper command = null;
        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sbatchCmd, _logger);
            var scheduledJobIds = _convertor.GetJobIds(command.Result).ToList();
            
            var schedulerJobIdClusterAllocationNamePairs = scheduledJobIds.Select(id => (id, jobSpecification.Tasks.First().ClusterNodeType.ClusterAllocationName)).ToList();

            IEnumerable<SubmittedTaskInfo> tasks = null;
            int maxRetries = clusterConfig.Scripts.EventualConsistencyRetryCount;
            int retryDelayMs = clusterConfig.Scripts.EventualConsistencyRetryDelayMs;
            int retryCount = maxRetries;
            while (retryCount >= 0)
            {
                try
                {
                    tasks = await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, schedulerJobIdClusterAllocationNamePairs);
                    if (tasks.Count() >= schedulerJobIdClusterAllocationNamePairs.Count)
                        return tasks;
                }
                catch (SlurmException) when (retryCount > 0)
                {
                    // eventual consistency: wait and retry
                }

                if (retryCount > 0)
                {
                    _logger.LogInformation($"Eventual consistency: only {tasks?.Count() ?? 0}/{schedulerJobIdClusterAllocationNamePairs.Count} tasks found in scontrol. Retrying in {retryDelayMs}ms... ({retryCount} attempts left)");
                    await Task.Delay(retryDelayMs);
                }
                retryCount--;
            }

            return tasks ?? await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, schedulerJobIdClusterAllocationNamePairs);
        }
        catch (Exception ex)
        {
            throw new SlurmException("SubmitJobException", ex, jobSpecification.Name, jobSpecification.Cluster.Name,
                command?.Error ?? ex.Message, command?.Result)
            {
                CommandError = command?.Error ?? ex.Message
            };
        }
    }

    /// <summary>
    ///     Get actual tasks
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="submitedTasksInfo">Submitted tasks ids</param>
    /// <param name="key">Key</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        var submitedTasksInfoList = submitedTasksInfo.ToList();
        try
        {
            return await GetActualTasksInfoAsync(connectorClient, cluster,
                submitedTasksInfoList.Select(s =>
                    (s.ScheduledJobId, s.Specification.ClusterNodeType.ClusterAllocationName)));
        }
        catch (SlurmException ex) when ((ex.CommandError != null && ex.CommandError.Contains("Invalid job id specified")) || (ex.Message != null && ex.Message.Contains("Invalid job id specified")))
        {
            _logger.LogWarning(
                $"At least one job in the batch is not in Slurm database. Querying tasks individually to isolate the invalid jobs.");
            
            var validTasks = new List<SubmittedTaskInfo>();
            foreach (var task in submitedTasksInfoList)
            {
                try
                {
                    var taskInfo = await GetActualTasksInfoAsync(connectorClient, cluster,
                        new[] { (task.ScheduledJobId, task.Specification.ClusterNodeType.ClusterAllocationName) });
                    validTasks.AddRange(taskInfo);
                }
                catch (SlurmException taskEx) when ((taskEx.CommandError != null && taskEx.CommandError.Contains("Invalid job id specified")) || (taskEx.Message != null && taskEx.Message.Contains("Invalid job id specified")))
                {
                    _logger.LogWarning(
                        $"Scheduled Job id: \"{task.ScheduledJobId}\" is not in Slurm scheduler database (Invalid job id specified). This job will be skipped in active query result (and marked as Failed).");
                    task.State = TaskState.Failed;
                    validTasks.Add(task);
                    
                }
            }
            return validTasks;
        }
    }

    /// <summary>
    ///     Cancel job
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="message">Message</param>
    public virtual async Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        string message)
    {
        StringBuilder cmdBuilder = new();
        foreach (var submitedTaskInfo in submitedTasksInfo)
        {
            var allocationCluster = string.Empty;

            if (!string.IsNullOrEmpty(submitedTaskInfo.Specification.ClusterNodeType.ClusterAllocationName))
                allocationCluster = $"-M {submitedTaskInfo.Specification.ClusterNodeType.ClusterAllocationName} ";

            cmdBuilder.Append(
                $"{_commands.InterpreterCommand} 'scancel {allocationCluster}{submitedTaskInfo.ScheduledJobId}';");
        }

        var sshCommand = cmdBuilder.ToString();
        _logger.LogInformation(
            $"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{sshCommand}\", message \"{message}\"",
            _logger);

        await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
    }

    /// <summary>
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="nodeType">Cluster node type</param>
    public virtual async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType)
    {
        SshCommandWrapper command = null;
        var allocationCluster = string.Empty;

        if (!string.IsNullOrEmpty(nodeType.ClusterAllocationName))
            allocationCluster = $"--clusters={nodeType.ClusterAllocationName} ";

        var sshCommand =
            $"{_commands.InterpreterCommand} 'sinfo -t alloc {allocationCluster}--partition={nodeType.Queue} -h -o \"%.6D\"'";
        _logger.LogInformation($"Get usage of queue \"{nodeType.Queue}\", command \"{sshCommand}\"");

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
            return _convertor.ReadQueueActualInformation(nodeType, command.Result);
        }
        catch (SlurmException ex)
        {
            throw new SlurmException("ClusterUsageException", ex, nodeType.Name, command.Result, command.Error)
            {
                CommandError = command.Error
            };
        }
    }

    /// <summary>
    ///     Get allocated nodes per task
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task information</param>
    public virtual async Task<IEnumerable<string>> GetAllocatedNodesAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        SshCommandWrapper command = null;
        StringBuilder cmdBuilder = new();

        var cluster = taskInfo.Specification.JobSpecification.Cluster;

        taskInfo.TaskAllocationNodes.ToList().ForEach(s =>
        {
            var fullDomain = $"{s.AllocationNodeId}.{cluster.DomainName ?? cluster.MasterNodeName}";
            var nodeId = s.AllocationNodeId;
            cmdBuilder.Append($"ip=$(dig +short {fullDomain}); [ -z \"$ip\" ] && ip=$(host {nodeId} | awk '{{print $NF}}'); echo $ip; ");
        });

        var sshCommand = cmdBuilder.ToString();
        _logger.LogInformation($"Get allocation nodes of task \"{taskInfo.Id}\", command \"{sshCommand}\"");
        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
            return command.Result
                .Split('\n')
                .Where(w => !string.IsNullOrEmpty(w))
                .Distinct()
                .ToList();
        }
        catch (SlurmException ex)
        {
            throw new SlurmException("GetAllocatedNodesException", ex, taskInfo.ScheduledJobId, command.Result,
                command.Error, sshCommand)
            {
                CommandError = command.Error
            };
        }
    }



    /// <summary>
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath)
    {
        return await _commands.GetParametersFromGenericUserScriptAsync(connectorClient, userScriptPath);
    }

    /// <summary>
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job info</param>
    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    /// <param name="projectAccountingString">Project accounting string</param>
    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        await _commands.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);
    }

    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        await _commands.CreateJobDirectoryAsync(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        return await _commands.DeleteJobDirectoryAsync(connectorClient, jobInfo, localBasePath);
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
    {
        await _commands.CopyJobDataToTempAsync(connectorClient, jobInfo, localBasePath, hash, path);
    }

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);
    }
    
    #endregion

    #region SSH tunnel methods

    /// <summary>
    ///     Create tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    /// <param name="nodePort">Cluster node port</param>
    public async Task CreateTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        await _sshTunnelUtil.CreateTunnelAsync(connectorClient, taskInfo.Id, nodeHost, nodePort);
    }

    /// <summary>
    ///     Remove tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task info</param>
    public async Task RemoveTunnelAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        await _sshTunnelUtil.RemoveTunnelAsync(connectorClient, taskInfo.Id);
    }

    /// <summary>
    ///     Get tunnels information
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        return _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);
    }

    #endregion
    
    #region Other methods

    /// <summary>
    ///     Initialize Cluster Script Directory
    /// </summary>
    /// <param name="schedulerConnectionConnection">Connector</param>
    /// <param name="clusterProjectRootDirectory">Cluster project root path</param>
    /// <param name="overwriteExistingProjectRootDirectory">Cluster project root path</param>
    /// <param name="localBasepath">Cluster execution path</param>
    /// <param name="isServiceAccount">Is servis account</param>
    /// <param name="account">Cluster username</param>
    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount, Dictionary<string, string>? customConfiguration)
    {
        return await _commands.InitializeClusterScriptDirectoryAsync(schedulerConnectionConnection, clusterProjectRootDirectory,
            overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount, customConfiguration);
    }

    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        return await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
    }

    private static string PrepareSbatchCommand(
        string script_name,
        string job_name, string account, string partition,
        int nodes, int ntasks_per_node, TimeSpan? time,
        string output, string error, bool isGpuPartition
    )
    {
        if (time == null)
            time = TimeSpan.FromMinutes(1);
        var result = "sbatch";
        result += " --job-name=" + job_name;
        result += " --account=" + account;
        result += " --partition=" + partition;
        result += " --nodes=" + nodes;
        result += " --ntasks-per-node=" + ntasks_per_node;
        if (isGpuPartition)
        {
            result += " --gpus=" + Math.Max(1, nodes);
        }
        result += " --time=" + $"{time:hh\\:mm\\:ss}";
        result += " --output=" + output;
        result += " --error=" + error;
        result += " --test-only " + script_name;
        return result;
    }

    public async Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog)
    {
        SshCommandWrapper command;
        
        Cluster cluster = clusterProjectCredential.ClusterProject.Cluster;
        Project project = clusterProjectCredential.ClusterProject.Project;

        int clusterConnectionFailedCount = 0;
        int dryRunJobFailedCount = 0;

        foreach (var nodeType in cluster.NodeTypes)
        {
            var partition = nodeType.Queue;
            var clusterConfig = ClusterRuntimeConfiguration.For(cluster.CustomConfiguration);
            var script_name = clusterConfig.GetExecuteCmdScriptPath(project.AccountingString, clusterProjectCredential.ClusterAuthenticationCredentials?.Username);
            var testCommand = PrepareSbatchCommand(
                script_name,
                job_name: "dryrun",
                account: project.AccountingString,
                partition: partition,
                nodes: 1,
                ntasks_per_node: 1,
                time: TimeSpan.FromSeconds(1),
                output: "dummy.out",
                error: "dummy.err",
                isGpuPartition: nodeType.ClusterNodeTypeAggregation != null && (nodeType.ClusterNodeTypeAggregation.AllocationType.Contains("ACN", StringComparison.OrdinalIgnoreCase) || nodeType.ClusterNodeTypeAggregation.AllocationType.Contains("GPU", StringComparison.OrdinalIgnoreCase))
            ) + "\n";
            var sshCommand = $"{_commands.InterpreterCommand} eval `(" + testCommand + ")`";
            sshCommand = sshCommand.Replace("\r\n", "\n").Replace("\r", "\n");
            try
            {
                command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sshCommand, _logger);
                checkLog.VaultCredentialOk = true;
                checkLog.ClusterConnectionOk = true;
                if (command.ExitStatus == 0)
                {
                    checkLog.DryRunJobOk = true;
                }
                else
                {
                    checkLog.DryRunJobOk = false;
                    checkLog.ErrorMessage += command.Error + "\n";
                    ++dryRunJobFailedCount;
                }
            }
            catch (SshCommandException e)
            {
                ++clusterConnectionFailedCount;
                checkLog.ErrorMessage += e.Message + "\n";
            }
            catch (Exception e)
            {
                checkLog.ErrorMessage += e.Message + "\n";
            }
        }

        if (clusterConnectionFailedCount > 0)
            checkLog.ClusterConnectionOk = false;

        if (dryRunJobFailedCount > 0)
            checkLog.DryRunJobOk = false;

        await Task.Delay(1);
        return null;
    }

    public async Task<DryRunJobInfo> DryRunJobAsync(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification)
    {
        var clusterConfig = ClusterRuntimeConfiguration.For(dryRunJobSpecification.ClusterNodeType.Cluster.CustomConfiguration);
        var sbatchCommand = PrepareSbatchCommand(
            clusterConfig.GetExecuteCmdScriptPath(dryRunJobSpecification.Project.AccountingString, dryRunJobSpecification.ClusterUser?.Username),
            job_name: "dryrun",
            account: dryRunJobSpecification.Project.AccountingString,
            partition: dryRunJobSpecification.ClusterNodeType.Queue,
            nodes: (int)dryRunJobSpecification.Nodes,
            ntasks_per_node: (int)dryRunJobSpecification.TasksPerNode,
            time: TimeSpan.FromMinutes(dryRunJobSpecification.WallTimeInMinutes),
            output: "dummy.out",
            error: "dummy.err",
            isGpuPartition:dryRunJobSpecification.IsGpuPartition
        ) + "\n";

        var sshCommand = $"{_commands.InterpreterCommand} eval `(" + sbatchCommand + ")`";
        sshCommand = sshCommand.Replace("\r\n", "\n").Replace("\r", "\n");

        //perform dry run
        SshCommandWrapper command =
            await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)schedulerConnectionConnection), sshCommand, _logger);
        
        var regex = new Regex(
            @"Job (\d+) to start at ([0-9T:-]+).*?using (\d+) processors on nodes (\S+) in partition (\S+)");
        //result goes to error stream in dry-run mode
        var match = regex.Match(command.Error);

        if (match.Success)
        {
            var info = new DryRunJobInfo
            {
                JobId = int.Parse(match.Groups[1].Value),
                StartTime = DateTime.Parse(match.Groups[2].Value),
                Processors = int.Parse(match.Groups[3].Value),
                Node = match.Groups[4].Value,
                Partition = match.Groups[5].Value,
                Message = command.Error
            };
            return info;
        }
        else
        {
            var info = new DryRunJobInfo
            {
                Message = !string.IsNullOrEmpty(command.Error) ? command.Error : command.Result
            };
            return info;
        }
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetHistoricalTasksInfoAsync(
        object schedulerConnectionConnection, 
        List<SubmittedTaskInfo> missingTasks,
        ClusterAuthenticationCredentials account)
    {
        if (missingTasks == null || !missingTasks.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var validTasks = missingTasks
            .Where(t => !string.IsNullOrEmpty(t.ScheduledJobId))
            .ToList();

        if (!validTasks.Any())
        {
            return Enumerable.Empty<SubmittedTaskInfo>();
        }

        var allHistoricalTasks = new List<SubmittedTaskInfo>();

        var groupedByAllocation = validTasks
            .GroupBy(t => t.Specification?.ClusterNodeType?.ClusterAllocationName ?? string.Empty);

        foreach (var allocationGroup in groupedByAllocation)
        {
            var clusterAllocationName = allocationGroup.Key;
            var tasksInGroup = allocationGroup.ToList();
            
            var jobIds = tasksInGroup.Select(t => t.ScheduledJobId).Distinct().ToList();
            string joinedJobIds = string.Join(",", jobIds);

            var allocationClusterFlag = string.Empty;
            if (!string.IsNullOrEmpty(clusterAllocationName))
            {
                allocationClusterFlag = $"-M {clusterAllocationName} ";
            }

            var sacctCmd = $"{_commands.InterpreterCommand} 'sacct -j {joinedJobIds} {allocationClusterFlag}--parsable2 --noheader --format=JobID,State'";
            _logger.LogInformation($"Bulk querying historical tasks via sacct for jobs: {joinedJobIds}");

            SshCommandWrapper command = null;
            try
            {
                command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)schedulerConnectionConnection), sacctCmd, _logger);
                _logger.LogDebug($"Raw sacct response: {command.Result}");

                if (string.IsNullOrWhiteSpace(command.Result))
                {
                    continue;
                }

                var lines = command.Result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var slurmStates = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        string rawJobId = parts[0].Trim();
                        string state = parts[1].Trim();

                        var cleanJobId = rawJobId.Split('.')[0];

                        if (!slurmStates.ContainsKey(cleanJobId))
                        {
                            slurmStates[cleanJobId] = state;
                        }
                    }
                }

                foreach (var task in tasksInGroup)
                {
                    if (slurmStates.TryGetValue(task.ScheduledJobId, out string slurmState))
                    {
                        var updatedTask = new SubmittedTaskInfo
                        {
                            Id = task.Id,
                            ScheduledJobId = task.ScheduledJobId,
                            State = MapSlurmStateToTaskState(slurmState),
                            Specification = task.Specification
                        };
                        allHistoricalTasks.Add(updatedTask);
                    }
                    else
                    {
                        var failedTask = new SubmittedTaskInfo
                        {
                            Id = task.Id,
                            ScheduledJobId = task.ScheduledJobId,
                            State = TaskState.Failed,
                            Specification = task.Specification
                        };
                        allHistoricalTasks.Add(failedTask);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to bulk retrieve historical tasks info via sacct for jobs: {joinedJobIds}. Error: {command?.Error}");
            }
        }

        return allHistoricalTasks;
    }

    private TaskState MapSlurmStateToTaskState(string slurmState)
    {
        if (string.IsNullOrEmpty(slurmState)) return TaskState.Failed;

        if (slurmState.StartsWith("COMPLETED")) return TaskState.Finished;
        if (slurmState.StartsWith("FAILED")) return TaskState.Failed;
        if (slurmState.StartsWith("CANCELLED") || slurmState.StartsWith("REVOKED")) return TaskState.Canceled;
        if (slurmState.StartsWith("TIMEOUT")) return TaskState.Failed;
        if (slurmState.StartsWith("NODE_FAIL")) return TaskState.Failed;
        if (slurmState.StartsWith("PREEMPTED")) return TaskState.Failed;
        if (slurmState.StartsWith("OUT_OF_MEMORY")) return TaskState.Failed;

        return TaskState.Failed;
    }

    public Task<string> GetMachineArchitectureAsync(object connectorClient, HEAppE.DomainObjects.ClusterInformation.Cluster cluster, int machineId)
    {
        throw new NotSupportedException("GetMachineArchitecture is not supported by Slurm");
    }

    public Task<string> GetMachineCalibrationAsync(object connectorClient, HEAppE.DomainObjects.ClusterInformation.Cluster cluster, int machineId, string calibrationId, string endpoint)
    {
        throw new NotSupportedException("GetMachineCalibration is not supported by Slurm");
    }

    #endregion
}
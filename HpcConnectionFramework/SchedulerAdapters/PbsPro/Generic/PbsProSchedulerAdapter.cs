using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;

/// <summary>
///     PBS Professional scheduler adapter
/// </summary>
public class PbsProSchedulerAdapter : ISchedulerAdapter
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="convertor">Convertor</param>
    public PbsProSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _sshTunnelUtil = new SshTunnelUtils();
        _commands = new LinuxCommands(logger);
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

    /// <summary>
    ///     Generic commnad key parameter
    /// </summary>
    protected static readonly string _genericCommandKeyParameter =
        HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

    #endregion

    #region ISchedulerAdapter Members

    /// <summary>
    ///     Submit job to scheduler
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    /// <exception cref="PbsException"></exception>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        var jobIdsWithJobArrayIndexes = new List<string>();
        SshCommandWrapper command = null;

        var sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub  -koed");
        _logger.LogInformation($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");
        var sshCommandBase64 =
            $"{_commands.InterpreterCommand} '{HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(jobSpecification.Project.AccountingString)} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64, _logger);
            var jobIds = _convertor.GetJobIds(command.Result).ToList();

            for (var i = 0; i < jobSpecification.Tasks.Count; i++)
                jobIdsWithJobArrayIndexes.AddRange(string.IsNullOrEmpty(jobSpecification.Tasks[i].JobArrays)
                    ? new List<string> { jobIds[i] }
                    : CombineScheduledJobIdWithJobArrayIndexes(jobIds[i], jobSpecification.Tasks[i].JobArrays));

            IEnumerable<SubmittedTaskInfo> tasks = null;
            int retryCount = 3;
            while (retryCount >= 0)
            {
                try
                {
                    tasks = await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, jobIdsWithJobArrayIndexes);
                    if (tasks.Count() >= jobIdsWithJobArrayIndexes.Count && 
                        tasks.All(t => !string.IsNullOrEmpty(t.Name) && t.State > TaskState.Configuring))
                    {
                        return tasks;
                    }
                }
                catch (PbsException) when (retryCount > 0)
                {
                    // eventual consistency: wait and retry
                }
                
                if (retryCount > 0)
                {
                    _logger.LogInformation($"Eventual consistency: only {tasks?.Count() ?? 0}/{jobIdsWithJobArrayIndexes.Count} tasks found with complete info in qstat. Retrying in 1s... ({retryCount} attempts left)");
                    await Task.Delay(1000);
                }
                retryCount--;
            }
            
            var resultTasks = (tasks ?? await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, jobIdsWithJobArrayIndexes)).ToList();
            
            _logger.LogInformation($"SubmitJob cleanup: {resultTasks.Count} tasks found in qstat response.");

            // Create placeholder DB tasks for enforcement (we only have jobSpecification here)
            var dbTasks = jobSpecification.Tasks.Select((t, i) => new SubmittedTaskInfo 
            { 
                ScheduledJobId = jobIds[i], 
                Specification = t 
            }).ToList();

            EnforceMetadataAndLog(resultTasks, dbTasks, "SubmitJob");
            
            return resultTasks;
        }
        catch (PbsException ex)
        {
            throw new PbsException("SubmitJobException", ex, jobSpecification.Name, jobSpecification.Cluster.Name,
                command?.Result, command?.Error, sshCommandBase64)
            {
                CommandError = command?.Error
            };
        }
    }

    /// <summary>
    ///     Get actual tasks (HPC jobs) informations
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="PbsException"></exception>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        var jobIdsWithJobArrayIndexes = Enumerable.Empty<string>();
        try
        {
            jobIdsWithJobArrayIndexes = submitedTasksInfo.SelectMany(s =>
                string.IsNullOrEmpty(s.Specification.JobArrays)
                    ? new List<string> { s.ScheduledJobId }
                    : CombineScheduledJobIdWithJobArrayIndexes(s.ScheduledJobId, s.Specification.JobArrays));

            var result = (await GetActualTasksInfoAsync(connectorClient, cluster, jobIdsWithJobArrayIndexes)).ToList();
            
            EnforceMetadataAndLog(result, submitedTasksInfo, "RefreshState");
            
            return result;
        }
        catch (SshCommandException ce)
        {
            //Reducing unknown job ids!
            var missingJobIds = new List<string>();
            foreach (Match match in Regex.Matches(ce.Message, @"(?<ErrorMessage>.*)\n", RegexOptions.Compiled))
                if (match.Success)
                {
                    var jobErrResponseMessage = match.Groups.GetValueOrDefault("ErrorMessage").Value;

                    missingJobIds.AddRange(Regex
                        .Matches(jobErrResponseMessage,
                            @"(qstat: Unknown Job Id )(?<JobId>(\d*(\[[0-9]*\])*)?(.[a-z-]*[\d]*)?)",
                            RegexOptions.Compiled)
                        .Where(w => w.Success && !string.IsNullOrEmpty(w.Groups.GetValueOrDefault("JobId").Value))
                        .Select(s => s.Groups.GetValueOrDefault("JobId").Value));
                }

            _logger.LogWarning(
                $"Scheduled Job ids: \"{missingJobIds}\" are not in PBS Professional scheduler database. Mentioned jobs were canceled!");
            var reducedjobIdsWithJobArrayIndexes = jobIdsWithJobArrayIndexes.Except(missingJobIds);
            if (!missingJobIds.Any() || reducedjobIdsWithJobArrayIndexes.Count() >= jobIdsWithJobArrayIndexes.Count())
                throw new PbsException(ce.Message, ce.Args)
                {
                    CommandError = null
                };

            if (!reducedjobIdsWithJobArrayIndexes.Any()) return Enumerable.Empty<SubmittedTaskInfo>();

            return await GetActualTasksInfoAsync(connectorClient, cluster, reducedjobIdsWithJobArrayIndexes);
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
        try
        {
            submitedTasksInfo.ToList().ForEach(f =>
                cmdBuilder.Append($"{_commands.InterpreterCommand} 'qdel {f.ScheduledJobId}';"));
            var sshCommand = cmdBuilder.ToString();
            _logger.LogInformation(
                $"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{sshCommand}\", message \"{message}\"");

            await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
        }
        catch (SshCommandException ce)
        {
            if (!ce.Contains("qdel: Job has finished") && !ce.Contains("qdel: Unknown Job Id")) throw;
        }
    }

    /// <summary>
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="nodeType">Cluster node type</param>
    /// <returns></returns>
    /// <exception cref="PbsException"></exception>
    public virtual async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType)
    {
        SshCommandWrapper command = null;
        var sshCommand = $"{_commands.InterpreterCommand} 'qstat -Q -f {nodeType.Queue}'";
        _logger.LogInformation($"Get usage of queue \"{nodeType.Queue}\", command \"{sshCommand}\"");

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
            return _convertor.ReadQueueActualInformation(nodeType, command.Result);
        }
        catch (PbsException ex)
        {
            throw new PbsException("ClusterUsageException", ex, nodeType.Name, command.Result, command.Error, sshCommand)
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
        catch (PbsException ex)
        {
            throw new PbsException("GetAllocatedNodesException", ex, taskInfo.ScheduledJobId, command.Result, command.Error,
                sshCommand)
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
    ///     Allow direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job information</param>
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
    /// <param name="jobInfo">Job information</param>
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
    /// <param name="jobInfo">Job information</param>
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
    /// <param name="jobInfo">Job information</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataFromTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);
    }

    #region SSH tunnel methods

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
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
    {
        return await _commands.InitializeClusterScriptDirectoryAsync(schedulerConnectionConnection, clusterProjectRootDirectory,
            overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount);
    }

    #endregion

    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        return await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
    }
    #endregion

    #region Private Methods

    /// <summary>
    ///     Get actual tasks (HPC jobs) informations
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="scheduledJobIds">Scheduler job id´s</param>
    /// <returns></returns>
    /// <exception cref="PbsException"></exception>
    private async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<string> scheduledJobIds)
    {
        SshCommandWrapper command = null;
        StringBuilder cmdBuilder = new();
        _logger.LogInformation($"Getting actual tasks information for jobs: \"{string.Join(", ", scheduledJobIds)}\"");

        cmdBuilder.Append($"{_commands.InterpreterCommand} 'qstat -f -x {string.Join(" ", scheduledJobIds)}'");
        var sshCommand = cmdBuilder.ToString();

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sshCommand, _logger);
            _logger.LogDebug($"Raw scheduler response for jobs {string.Join(", ", scheduledJobIds)}: {command.Result}");
            var submittedTasksInfo = _convertor.ReadParametersFromResponse(cluster, command.Result).ToList();
            _logger.LogInformation($"Successfully retrieved information for {submittedTasksInfo.Count} tasks.");
            return submittedTasksInfo;
        }
        catch (PbsException ex)
        {
            _logger.LogError(ex, $"Failed to get actual tasks info for jobs: {string.Join(", ", scheduledJobIds)}. Result: {command?.Result}, Error: {command?.Error}");
            throw new PbsException("GetActualTasksInfo", ex, string.Join(", ", scheduledJobIds), command.Result,
                command.Error, sshCommand)
            {
                CommandError = command.Error
            };
        }
    }

    private void EnforceMetadataAndLog(List<SubmittedTaskInfo> clusterTasks, IEnumerable<SubmittedTaskInfo> dbTasks, string context)
    {
        var dbTasksList = dbTasks.ToList();
        _logger.LogInformation($"[{context}] Validating {clusterTasks.Count} cluster tasks against {dbTasksList.Count} DB tasks.");
        
        foreach (var clusterTask in clusterTasks)
        {
            var oldState = clusterTask.State;
            // Always ensure at least Submitted state if we found it in qstat
            if (clusterTask.State == TaskState.Configuring)
                clusterTask.State = TaskState.Submitted;

            if (string.IsNullOrEmpty(clusterTask.Name))
            {
                foreach (var dbTask in dbTasksList)
                {
                    var originalJobId = dbTask.ScheduledJobId;
                    if (clusterTask.ScheduledJobId == originalJobId || 
                        (originalJobId != null && originalJobId.EndsWith("[]") && 
                         clusterTask.ScheduledJobId.StartsWith(originalJobId.Replace("[]", "["))))
                    {
                        if (dbTask.Specification != null)
                        {
                            clusterTask.Name = dbTask.Specification.Id.ToString();
                            _logger.LogInformation($"[{context}] Enforced name mapping for task {clusterTask.ScheduledJobId}: {clusterTask.Name} (State: {oldState}->{clusterTask.State})");
                            break;
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(clusterTask.Name))
                {
                    _logger.LogWarning($"[{context}] Could not find mapping for cluster task {clusterTask.ScheduledJobId} (State: {clusterTask.State})");
                }
            }
            else
            {
                _logger.LogInformation($"[{context}] Task {clusterTask.ScheduledJobId} has name: {clusterTask.Name} (State: {clusterTask.State})");
            }
        }
    }

    /// <summary>
    ///     Create all scheduled job id´s combinated (jobarray indexes)
    /// </summary>
    /// <param name="scheduledJobId">Scheduled job id</param>
    /// <param name="jobArrayParameter">Jobarray parameter for job</param>
    /// <returns></returns>
    private static IEnumerable<string> CombineScheduledJobIdWithJobArrayIndexes(string scheduledJobId,
        string jobArrayParameter)
    {
        var combJobIdAndJobArrayIndex = new List<string> { scheduledJobId };
        var jobArraysParameters = Regex.Split(jobArrayParameter, @"\D+").Select(x => int.Parse(x))
            .ToList();

        var minIndex = jobArraysParameters[0];
        var maxIndex = jobArraysParameters[1];
        var step = jobArraysParameters.Count == 3 ? jobArraysParameters[2] : 1;

        for (var i = minIndex; i <= maxIndex; i += step)
            combJobIdAndJobArrayIndex.Add(scheduledJobId.Replace("[]", $"[{i}]"));

        return combJobIdAndJobArrayIndex;
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
            var script_name = HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(project.AccountingString);
            var testCommand = $"[ -f {script_name} ]"; // just check that file to run scripts exists
            var sshCommand = $"{_commands.InterpreterCommand} eval `(" + testCommand + ")`";
            sshCommand = sshCommand.Replace("\r\n", "\n").Replace("\r", "\n");
            try
            {
                command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
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

    public Task<DryRunJobInfo> DryRunJobAsync(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification)
    {
        // Currently not implemented for PBS Pro
        throw new NotSupportedException("Dry run job is not supported for PBS Pro scheduler.");
    }

    #endregion
}
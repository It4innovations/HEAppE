using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
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

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;

/// <summary>
///     Local Linux HPC Scheduler Adapter
/// </summary>
public class LinuxLocalSchedulerAdapter : ISchedulerAdapter
{
    #region Constructors

    /// <summary>
    ///     Constructs Linux Local scheduler adapeter
    /// </summary>
    /// <param name="convertor">Convertor</param>
    public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
    {
        _logger = logger;
        _convertor = convertor;
        _commands = new LinuxCommands(_logger);
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Get actual tasks info
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="scheduledJobIds">Scheduled Job ID collection</param>
    /// <param name="account">Username name</param>
    /// <returns></returns>
    private async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<string> scheduledJobIds, string account)
    {
        var submittedTaskInfos = new List<SubmittedTaskInfo>();
        var scheduledJobIdsList = scheduledJobIds.Select(x => x).Distinct();
        foreach (var jobId in scheduledJobIdsList)
        {
            var jobDirPath = Path.Combine(_scripts.InstanceIdentifierPath, HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath, account, jobId)
                .Replace('\\', '/');
            var cliCommand =
                $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.GetJobInfoCmdScriptName} {jobDirPath}";
            var command = await SshCommandUtils.RunSshCommandAsync(connectorClient, cliCommand, _logger);

            _logger.LogInformation($"Get actual task info id=\"{jobId}\", command \"{cliCommand}\", result \"{command.Result}\"");
            submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(cluster, command.Result));
        }

        return submittedTaskInfos;
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    protected ILogger _logger;

    /// <summary>
    ///     Convertor reference.
    /// </summary>
    protected ISchedulerDataConvertor _convertor;

    /// <summary>
    ///     Linux SSH Commands
    /// </summary>
    protected ICommands _commands;

    /// <summary>
    ///     Command Script Paths
    /// </summary>
    protected readonly CommandScriptPathConfiguration _commandScripts =
        HPCConnectionFrameworkConfiguration.ScriptsSettings.CommandScriptsPathSettings;

    /// <summary>
    ///     Command
    /// </summary>
    protected readonly LinuxLocalCommandScriptPathConfiguration _linuxLocalCommandScripts =
        HPCConnectionFrameworkConfiguration.ScriptsSettings.LinuxLocalCommandScriptPathSettings;

    /// <summary>
    ///     Generic command key parameter
    /// </summary>
    protected static readonly string _genericCommandKeyParameter =
        HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    /// <summary>
    ///     Localhost used as DomainName when not supported from cluster
    /// </summary>
    private readonly string LocalDomainName = "localhost";

    #endregion

    #region ISchedulerAdapter Members

    /// <summary>
    ///     Submit job to scheduler
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        var shellCommandSb = new StringBuilder();
        SshCommandWrapper command = null;

        string account = jobSpecification.ClusterUser.Username;

        var shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);
        _logger.LogInformation($"Submitting job \"{jobSpecification.Id}\", command \"{shellCommand}\"");
        var sshCommandBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand));

        command = await SshCommandUtils.RunSshCommandAsync(connectorClient,
            $"{HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(jobSpecification.Project.AccountingString)} {sshCommandBase64}",
            _logger);

        shellCommandSb.Clear();
        var localBasePath = jobSpecification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobSpecification.ProjectId)?.ScratchStoragePath;

        //compose command with parameters of job and task IDs
        shellCommandSb.Append(
            $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.RunLocalCmdScriptName} {localBasePath}/{_scripts.InstanceIdentifierPath}/{HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath}/{account}/{jobSpecification.Id}/");
        jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

        //log local HPC Run script to log file
        shellCommandSb.Append(
            $" >> {localBasePath}/{_scripts.InstanceIdentifierPath}/{HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath}/{account}/{jobSpecification.Id}/job_logger.Logtxt");
        shellCommand = shellCommandSb.ToString();

        sshCommandBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand));
        command = await SshCommandUtils.RunSshCommandAsync(connectorClient,
            $"{HPCConnectionFrameworkConfiguration.GetPathToScript(jobSpecification.Project.AccountingString, "run_background_command.sh")} {sshCommandBase64}",
            _logger);

        return await GetActualTasksInfoAsync(connectorClient, jobSpecification.Cluster, new[] { $"{jobSpecification.Id}" }, jobSpecification.ClusterUser.Username);
    }

    /// <summary>
    ///     Get actual tasks
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="submitedTasksInfo">Submitted tasks ids</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        var localClusterJobIds = submitedTasksInfo.Select(s => s.Specification.JobSpecification.Id.ToString())
            .Distinct();

        return await GetActualTasksInfoAsync(connectorClient, cluster, localClusterJobIds,  key);
    }

    /// <summary>
    ///     Cancel job
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="message">Message</param>
    public virtual async Task CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
        string message)
    {
        StringBuilder commandSb = new();
        var localClusterJobIds = submitedTasksInfo.Select(s => s.Specification.JobSpecification.Id.ToString())
            .Distinct();
        localClusterJobIds.ToList().ForEach(id =>
            commandSb.Append(
                $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.CancelJobCmdScriptName} {Path.Combine(_scripts.SubExecutionsPath, id.ToString()).Replace('\\', '/')};"));
        var command = commandSb.ToString();

        _logger.LogInformation(
            $"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{command}\", message \"{message}\"");
        await SshCommandUtils.RunSshCommandAsync(connectorClient, command, _logger);
    }

    /// <summary>
    ///     Get cluster node usage
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="nodeType">ClusterNode type</param>
    /// <returns></returns>
    public virtual async Task<ClusterNodeUsage> GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
    {
        var usage = new ClusterNodeUsage
        {
            NodeType = nodeType
        };

        var command = await SshCommandUtils.RunSshCommandAsync(connectorClient,
            $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.CountJobsCmdScriptName}",
            _logger);
        _logger.LogInformation($"Get usage of queue \"{nodeType.Queue}\", command \"{command}\"");
        if (int.TryParse(command.Result, out var totalJobs)) usage.TotalJobs = totalJobs;

        return usage;
    }

    /// <summary>
    ///     Get allocated nodes per task
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task information</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public virtual async Task<IEnumerable<string>> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        List<string> allocatedNodes = new();
        StringBuilder allocationNodeSb = new();

        allocationNodeSb.Clear();
        allocationNodeSb.Append(taskInfo.Specification.ClusterNodeType.Cluster.DomainName ?? LocalDomainName);

        if (taskInfo.NodeType.Cluster.Port.HasValue)
            allocationNodeSb.Append($":{taskInfo.NodeType.Cluster.Port.Value}");

        allocatedNodes.Add(allocationNodeSb.ToString());
        _logger.LogInformation($"Get allocation nodes of task \"{taskInfo.Id}\"");
        await Task.Yield();
        return allocatedNodes.Distinct();
    }

    /// <summary>
    ///     Get generic command templates parameters from script
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="userScriptPath">Generic script path</param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<string>> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
    {
        return await _commands.GetParametersFromGenericUserScriptAsync(connectorClient, userScriptPath);
    }

    /// <summary>
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job info</param>
    public async Task AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    ///  <param name="projectAccountingString">Project accounting string</param>
    public async Task RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
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
    public virtual async Task CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        await _commands.CreateJobDirectoryAsync(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    public async Task<bool> DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        return await _commands.DeleteJobDirectoryAsync(connectorClient, jobInfo, localBasePath);
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public async Task CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
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
    public async Task CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        await _commands.CopyJobDataFromTempAsync(connectorClient, jobInfo, localBasePath, hash);
    }

    #region SSH tunnel methods

    /// <summary>
    ///     Create tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    /// <param name="nodePort">Cluster node port</param>
    public async Task CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        await Task.Delay(1);
        throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
    }


    /// <summary>
    ///     Remove tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task info</param>
    public async Task RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        await Task.Delay(1);
        throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
    }

    /// <summary>
    ///     Get tunnels information
    /// </summary>
    /// <param name="taskInfo">Task info</param>
    /// <param name="nodeHost">Cluster node address</param>
    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
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
    public async Task<bool> InitializeClusterScriptDirectory(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
    {
        return await _commands.InitializeClusterScriptDirectoryAsync(schedulerConnectionConnection, clusterProjectRootDirectory,
            overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount);
    }

    #endregion
    
    public async Task<bool> MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations, bool sharedAccountsPoolMode)
    {
        return await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations, sharedAccountsPoolMode);
    }

    public async Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog)
    {
        SshCommandWrapper command;
        Cluster cluster = clusterProjectCredential.ClusterProject.Cluster;

        int clusterConnectionFailedCount = 0;
        int dryRunJobFailedCount = 0;

        foreach (var nodeType in cluster.NodeTypes)
        {
            var partition = nodeType.Queue;
            var script_name = $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.RunLocalCmdScriptName}";
            var testCommand = $"[ -f {script_name} ]"; // just check that file to run scripts exists
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

    public Task<DryRunJobInfo> DryRunJob(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification)
    {
        // For local Linux scheduler, we can just return a success message
        //return "Dry run simulation successful for Linux Local Scheduler.";
        return Task.FromResult(new DryRunJobInfo
        {
            Message = "Dry run simulation successful for Linux Local Scheduler."
        });
    }

    #endregion
}
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
using log4net;
using Org.BouncyCastle.Tls;
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
    public SlurmSchedulerAdapter(ISchedulerDataConvertor convertor)
    {
        _log = LogManager.GetLogger(typeof(SlurmSchedulerAdapter));
        _convertor = convertor;
        _sshTunnelUtil = new SshTunnelUtils();
        _commands = new LinuxCommands();
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Get actual tasks (HPC jobs) information
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="cluster">Cluster"</param>
    /// <param name="schedulerJobIdClusterAllocationNamePairs">Scheduler job id´s pair</param>
    /// <returns></returns>
    /// <exception cref="SlurmException"></exception>
    private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<(string ScheduledJobId, string ClusterAllocationName)> schedulerJobIdClusterAllocationNamePairs)
    {
        SshCommandWrapper command = null;
        StringBuilder cmdBuilder = new();

        foreach (var (ScheduledJobId, ClusterAllocationName) in schedulerJobIdClusterAllocationNamePairs)
        {
            var allocationCluster = string.Empty;

            if (!string.IsNullOrEmpty(ClusterAllocationName)) allocationCluster = $"-M {ClusterAllocationName} ";

            cmdBuilder.Append(
                $"{_commands.InterpreterCommand} 'scontrol show JobId {allocationCluster}{ScheduledJobId} -o';");
        }

        var sshCommand = cmdBuilder.ToString();

        try
        {
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
            var submittedTasksInfo = _convertor.ReadParametersFromResponse(cluster, command.Result);
            return submittedTasksInfo;
        }
        catch (SlurmException)
        {
            throw new SlurmException(
                "GetActualTasksInfo",
                string.Join(", ", schedulerJobIdClusterAllocationNamePairs.Select(s => s.ScheduledJobId).ToList()),
                command.Result,
                command.Error,
                sshCommand)
            {
                CommandError = command.Error
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
    protected ILog _log;

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
    public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        // 1. Prepare the primary job submission command (sbatch)
        var sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "sbatch");
        _log.Info($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");

        // 2. Wrap the command into the interpreter and helper script (Base64 encoded)
        var sbatchCmd = $"{_commands.InterpreterCommand} '{HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(jobSpecification.Project.AccountingString)} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";

        // Improved Chaining:
        // 1. We use 'grep -oE "[0-9]+"' to ensure we only pass numeric Job IDs to scontrol.
        // 2. We use 'xargs -r' (or --no-run-if-empty) to prevent running scontrol if no ID is found.
        var integratedCommand = $"{sbatchCmd} | grep -oE '[0-9]+' | xargs -r -n 1 -I {{}} {_commands.InterpreterCommand} 'scontrol show JobId={{}} -o'";

        SshCommandWrapper command = null;
        try
        {
            // Execute the combined command in a single SSH session
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), integratedCommand);
        
            // Parse the detailed job information directly from the combined output
            return _convertor.ReadParametersFromResponse(jobSpecification.Cluster, command.Result);
        }
        catch (Exception ex)
        {
            // Ensure detailed error reporting if the cluster communication fails
            throw new SlurmException("SubmitJobException", jobSpecification.Name, jobSpecification.Cluster.Name,
                command?.Error ?? ex.Message, command?.Result, integratedCommand)
            {
                CommandError = command?.Error
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
    public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        var submitedTasksInfoList = submitedTasksInfo.ToList();
        try
        {
            return GetActualTasksInfo(connectorClient, cluster,
                submitedTasksInfoList.Select(s =>
                    (s.ScheduledJobId, s.Specification.ClusterNodeType.ClusterAllocationName)));
        }
        catch (SshCommandException)
        {
            _log.Warn(
                $"Scheduled Job ids: \"{string.Join(",", submitedTasksInfoList.Select(s => s.ScheduledJobId))}\" are not in Slurm scheduler database. Mentioned jobs were canceled!");
            return Enumerable.Empty<SubmittedTaskInfo>();
        }
    }

    /// <summary>
    ///     Cancel job
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
    /// <param name="message">Message</param>
    public virtual void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo,
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
        _log.Info(
            $"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{sshCommand}\", message \"{message}\"");

        SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
    }

    /// <summary>
    ///     Get actual scheduler queue status
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="nodeType">Cluster node type</param>
    public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
    {
        SshCommandWrapper command = null;
        var allocationCluster = string.Empty;

        if (!string.IsNullOrEmpty(nodeType.ClusterAllocationName))
            allocationCluster = $"--clusters={nodeType.ClusterAllocationName} ";

        var sshCommand =
            $"{_commands.InterpreterCommand} 'sinfo -t alloc {allocationCluster}--partition={nodeType.Queue} -h -o \"%.6D\"'";
        _log.Info($"Get usage of queue \"{nodeType.Queue}\", command \"{sshCommand}\"");

        try
        {
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
            return _convertor.ReadQueueActualInformation(nodeType, command.Result);
        }
        catch (SlurmException)
        {
            throw new SlurmException("ClusterUsageException", nodeType.Name, command.Result, command.Error, sshCommand)
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
    public virtual IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
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
        _log.Info($"Get allocation nodes of task \"{taskInfo.Id}\", command \"{sshCommand}\"");
        try
        {
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
            return command.Result
                .Split('\n')
                .Where(w => !string.IsNullOrEmpty(w))
                .Distinct()
                .ToList();
        }
        catch (SlurmException)
        {
            throw new SlurmException("GetAllocatedNodesException", taskInfo.ScheduledJobId, command.Result,
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
    public virtual IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
    {
        return _commands.GetParametersFromGenericUserScript(connectorClient, userScriptPath);
    }

    /// <summary>
    ///     Allow direct file transfer acces for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="jobInfo">Job info</param>
    public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
    }

    /// <summary>
    ///     Remove direct file transfer access for user
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="publicKeys">Public keys</param>
    /// <param name="projectAccountingString">Project accounting string</param>
    public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        _commands.RemoveDirectFileTransferAccessForUser(connectorClient, publicKeys, projectAccountingString);
    }

    /// <summary>
    ///     Create job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="localBasePath"></param>
    /// <param name="sharedAccountsPoolMode"></param>
    public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        _commands.CreateJobDirectory(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    public bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        return _commands.DeleteJobDirectory(connectorClient, jobInfo, localBasePath);
    }

    /// <summary>
    ///     Copy job data to temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
    {
        _commands.CopyJobDataToTemp(connectorClient, jobInfo, localBasePath, hash, path);
    }

    /// <summary>
    ///     Copy job data from temp folder
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="jobInfo">Job info</param>
    /// <param name="hash">Hash</param>
    public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
    {
        _commands.CopyJobDataFromTemp(connectorClient, jobInfo, localBasePath, hash);
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
    public void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        _sshTunnelUtil.CreateTunnel(connectorClient, taskInfo.Id, nodeHost, nodePort);
    }

    /// <summary>
    ///     Remove tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskInfo">Task info</param>
    public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        _sshTunnelUtil.RemoveTunnel(connectorClient, taskInfo.Id);
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
    public bool InitializeClusterScriptDirectory(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
    {
        return _commands.InitializeClusterScriptDirectory(schedulerConnectionConnection, clusterProjectRootDirectory,
            overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount);
    }

    public bool MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations)
    {
        return _commands.CopyJobFiles(schedulerConnectionConnection, jobInfo, sourceDestinations);
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
        result += " --time=" + $"{time:hh\\:mm\\:ss}";
        result += " --output=" + output;
        result += " --error=" + error;
        result += isGpuPartition? $" --gpus={nodes}" : "";
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
            var testCommand = PrepareSbatchCommand(
                HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(project.AccountingString),
                job_name: "dryrun",
                account: project.AccountingString,
                partition: partition,
                nodes: 1,
                ntasks_per_node: 1,
                time: TimeSpan.FromSeconds(1),
                output: "dummy.out",
                error: "dummy.err",
                isGpuPartition: nodeType.ClusterNodeTypeAggregation != null && (nodeType.ClusterNodeTypeAggregation.AllocationType.Contains("ACN") || nodeType.ClusterNodeTypeAggregation.AllocationType.Contains("GPU"))
            ) + "\n";
            var sshCommand = $"{_commands.InterpreterCommand} eval `(" + testCommand + ")`";
            sshCommand = sshCommand.Replace("\r\n", "\n").Replace("\r", "\n");
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
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

    public DryRunJobInfo DryRunJob(object schedulerConnectionConnection, DryRunJobSpecification dryRunJobSpecification)
    {
        var sbatchCommand = PrepareSbatchCommand(
            HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(dryRunJobSpecification.Project
                .AccountingString),
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
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)schedulerConnectionConnection), sshCommand);
        
        var regex = new Regex(
            @"Job (\d+) to start at ([0-9T:-]+) using (\d+) processors on nodes (\S+) in partition (\S+)");
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
                Message = command.Result
            };
            return info;
        }
            
            
    }

    #endregion
}
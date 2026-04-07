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

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic;

/// <summary>
///     HyperQueue scheduler adapter
/// </summary>
internal class HyperQueueSchedulerAdapter : ISchedulerAdapter
{
    #region Constructors

    public HyperQueueSchedulerAdapter(ISchedulerDataConvertor convertor, ILogger logger)
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

    #endregion

    #region ISchedulerAdapter Members

    public async Task<IEnumerable<SubmittedTaskInfo>> SubmitJobAsync(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        var schedulerJobIdClusterAllocationNamePairs =
            new List<(string ScheduledJobId, string ClusterAllocationName)>();
        SshCommandWrapper command = null;
        var sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "hq submit");
        _logger.LogInformation($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");
        var sshCommandBase64 =
            $"{_commands.InterpreterCommand} '{HPCConnectionFrameworkConfiguration.GetExecuteCmdScriptPath(jobSpecification.Project.AccountingString)} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";

        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
            //^Job submitted successfully, job ID: (\d+)$
            // implement parser regex
            var jobIdPattern = @"^Job submitted successfully, job ID: (\d+)$";
            var match = Regex.Match(command.Result, jobIdPattern);
            var jobId = match.Success ? match.Groups[1].Value : string.Empty;
            if (string.IsNullOrEmpty(jobId))
                throw new Exception(
                    $"Unable to parse job id from HQ server! Job id pattern: {jobIdPattern}, command result: {command.Result}");

            schedulerJobIdClusterAllocationNamePairs.Add((jobId,
                jobSpecification.Tasks.First().ClusterNodeType.ClusterAllocationName));

            var taskInfo = await GetActualHqJobInfoAsync(connectorClient, jobSpecification.Cluster, jobId);

            return new List<SubmittedTaskInfo> { taskInfo };
        }
        catch (FormatException e)
        {
            throw new Exception(
                @$"Exception thrown when submitting a job: ""{jobSpecification.Name}"" to the cluster: ""{jobSpecification.Cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommandBase64}"".\n", e);
        }
    }

    private async Task<SubmittedTaskInfo> GetActualHqJobInfoAsync(object connectorClient, Cluster cluster, string jobId)
    {
        //create hq command and send to hq job info 1 --output-mode=json
        var sshCommand = $"ml HyperQueue && hq job info {jobId} --output-mode=json";
        SshCommandWrapper command = null;
        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
            return _convertor.ReadParametersFromResponse(cluster, command.Result).First();
        }
        catch (FormatException e)
        {
            throw new Exception(
                @$"Exception thrown when getting job info for job id: ""{jobId}"" from the cluster: ""{cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}"".\n", e);
        }
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetActualTasksInfoAsync(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        var tasksInfo = new List<SubmittedTaskInfo>();
        foreach (var task in submitedTasksInfo)
        {
            var actualInfo = await GetActualHqJobInfoAsync(connectorClient, cluster, task.ScheduledJobId);
            if (string.IsNullOrEmpty(actualInfo.ScheduledJobId))
            {
                task.State = actualInfo.State;
                task.ErrorMessage = actualInfo.ErrorMessage;
                tasksInfo.Add(task);
                continue;
            }

            tasksInfo.Add(actualInfo);
        }

        return tasksInfo;
    }

    public async Task CancelJobAsync(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
    {
        var sshCommand =
            $"ml HyperQueue && hq job cancel {string.Join(", ", submitedTasksInfo.Select(s => s.ScheduledJobId))}";
        SshCommandWrapper command = null;
        try
        {
            command = await SshCommandUtils.RunSshCommandAsync(connectorClient, sshCommand, _logger);
        }
        catch (FormatException e)
        {
            throw new Exception(
                @$"Exception thrown when cancelling job with id: ""{string.Join(", ", submitedTasksInfo.Select(s => s.ScheduledJobId))}"".
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}"".\n", e);
        }
    }

    public Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(object connectorClient, ClusterNodeType nodeType)
    {
        throw new NotImplementedException("GetCurrentClusterNodeUsage is not supported for HyperQueue");
    }

    public Task<IEnumerable<string>> GetAllocatedNodesAsync(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        throw new NotImplementedException("GetAllocatedNodes is not supported for HyperQueue");
    }

    public virtual async Task<IEnumerable<string>> GetParametersFromGenericUserScriptAsync(object connectorClient, string userScriptPath)
    {
        return await _commands.GetParametersFromGenericUserScriptAsync(connectorClient, userScriptPath);
    }

    public async Task AllowDirectFileTransferAccessForUserToJobAsync(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        await _commands.AllowDirectFileTransferAccessForUserToJobAsync(connectorClient, publicKey, jobInfo);
    }

    public async Task RemoveDirectFileTransferAccessForUserAsync(object connectorClient, IEnumerable<string> publicKeys, string projectAccountingString)
    {
        await _commands.RemoveDirectFileTransferAccessForUserAsync(connectorClient, publicKeys, projectAccountingString);
    }

    public async Task CreateJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        await _commands.CreateJobDirectoryAsync(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
    }

    public async Task<bool> DeleteJobDirectoryAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        return await _commands.DeleteJobDirectoryAsync(connectorClient, jobInfo, localBasePath);
    }

    public async Task CopyJobDataToTempAsync(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
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

    public async Task<bool> InitializeClusterScriptDirectoryAsync(object schedulerConnectionConnection,
        string clusterProjectRootDirectory, bool overwriteExistingProjectRootDirectory,
        string localBasepath, string account, bool isServiceAccount)
    {
        return await _commands.InitializeClusterScriptDirectoryAsync(schedulerConnectionConnection, clusterProjectRootDirectory,
            overwriteExistingProjectRootDirectory, localBasepath, account, isServiceAccount);
    }
    public async Task<bool> MoveJobFilesAsync(object schedulerConnectionConnection, SubmittedJobInfo jobInfo, IEnumerable<Tuple<string, string>> sourceDestinations)
    {
        return await _commands.CopyJobFilesAsync(schedulerConnectionConnection, jobInfo, sourceDestinations);
    }

    public async Task<dynamic> CheckClusterAuthenticationCredentialsStatus(object connectorClient, ClusterProjectCredential clusterProjectCredential, ClusterProjectCredentialCheckLog checkLog)
    {
        int clusterConnectionFailedCount = 0;
        int dryRunJobFailedCount = 0;

        Cluster cluster = clusterProjectCredential.ClusterProject.Cluster;
        Project project = clusterProjectCredential.ClusterProject.Project;

        foreach (var nodeType in cluster.NodeTypes)
        {
            // Dummy solution for HyperQueue: just test that the command is available
            var testCommand = @$"echo ""{project.AccountingString}"" && ml HyperQueue && hq --version";
            var sshCommand = $"{_commands.InterpreterCommand} " + testCommand;
            sshCommand = sshCommand.Replace("\r\n", "\n").Replace("\r", "\n");
            try
            {
                SshCommandWrapper command = await SshCommandUtils.RunSshCommandAsync(new SshClientAdapter((SshClient)connectorClient), sshCommand, _logger);
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
        throw new NotSupportedException("DryRunJob is not supported for HyperQueue");
    }

    #endregion
}
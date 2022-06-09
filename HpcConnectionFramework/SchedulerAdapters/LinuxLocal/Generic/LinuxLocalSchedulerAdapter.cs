using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal
{
    /// <summary>
    /// Local Linux HPC Scheduler Adapter
    /// </summary>
    public class LinuxLocalSchedulerAdapter : ISchedulerAdapter
    {
        #region Instances
        /// <summary>
        ///   Log4Net logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        ///   Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;

        /// <summary>
        /// Linux SSH Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        /// Command Script Paths
        /// </summary>
        protected readonly CommandScriptPathConfiguration _commandScripts = HPCConnectionFrameworkConfiguration.CommandScriptsPathSettings;

        /// <summary>
        /// Command
        /// </summary>
        protected readonly LinuxLocalCommandScriptPathConfiguration _linuxLocalCommandScripts = HPCConnectionFrameworkConfiguration.LinuxLocalCommandScriptPathSettings;

        /// <summary>
        /// Generic commnad key parameter
        /// </summary>
        protected static readonly string _genericCommandKeyParameter = HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

        /// <summary>
        /// Localhost used as DomainName when not supported from cluster 
        /// </summary>
        private readonly string LocalDomainName = "localhost";
        #endregion
        #region Constructors
        /// <summary>
        /// Constructs Linux Local scheduler adapeter
        /// </summary>
        /// <param name="convertor">Convertor</param>
        public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _convertor = convertor;
            _log = LogManager.GetLogger(typeof(LinuxLocalSchedulerAdapter));
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        /// <summary>
        /// Submit job to scheduler
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobSpecification">Job specification</param>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var shellCommandSb = new StringBuilder();
            SshCommandWrapper command = null;

            string shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);
            _log.Info($"Submitting job \"{jobSpecification.Id}\", command \"{shellCommand}\"");
            string sshCommandBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand));

            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.ExecutieCmdPath} {sshCommandBase64}");

            shellCommandSb.Clear();

            //compose command with parameters of job and task IDs
            shellCommandSb.Append($"{_linuxLocalCommandScripts.RunLocalCmdPath} {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/");
            jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

            //log local HPC Run script to log file
            shellCommandSb.Append($" >> {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/job_log.txt &");
            shellCommand = shellCommandSb.ToString();

            sshCommandBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand));
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commandScripts.ExecutieCmdPath} {sshCommandBase64}");

            return GetActualTasksInfo(connectorClient, jobSpecification.Cluster, new string[] { $"{jobSpecification.Id}" });
        }

        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="cluster">Cluster</param>
        /// <param name="submitedTasksInfo">Submitted tasks ids</param>
        /// <returns></returns>
        public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var localClusterJobIds = submitedTasksInfo.Select(s => s.Specification.JobSpecification.Id.ToString())
                                                        .Distinct();
            return GetActualTasksInfo(connectorClient, cluster, localClusterJobIds);
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
        /// <param name="message">Message</param>
        public virtual void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
        {
            StringBuilder commandSb = new();
            var localClusterJobIds = submitedTasksInfo.Select(s => s.Specification.JobSpecification.Id.ToString())
                                                        .Distinct();

            localClusterJobIds.ToList().ForEach(id => commandSb.Append($"{_linuxLocalCommandScripts.CancelJobCmdPath} {id};"));
            string command = commandSb.ToString();

            _log.Info($"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{command}\", message \"{message}\"");
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), command);
        }

        /// <summary>
        /// Get cluster node usage
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="nodeType">ClusterNode type</param>
        /// <returns></returns>
        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            var usage = new ClusterNodeUsage
            {
                NodeType = nodeType
            };

            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), _linuxLocalCommandScripts.CountJobsCmdPath);
            _log.Info($"Get usage of queue \"{nodeType.Queue}\", command \"{command}\"");
            if (int.TryParse(command.Result, out int totalJobs))
            {
                usage.TotalJobs = totalJobs;
            }

            return usage;
        }

        /// <summary>
        /// Get allocated nodes per task
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task information</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            List<string> allocatedNodes = new();
            StringBuilder allocationNodeSb = new();

            allocationNodeSb.Clear();
            allocationNodeSb.Append(taskInfo.Specification.ClusterNodeType.Cluster.DomainName ?? LocalDomainName);

            if (taskInfo.NodeType.Cluster.Port.HasValue)
            {
                allocationNodeSb.Append($":{taskInfo.NodeType.Cluster.Port.Value}");
            }

            allocatedNodes.Add(allocationNodeSb.ToString());
            _log.Info($"Get allocation nodes of task \"{taskInfo.Id}\"");
            return allocatedNodes.Distinct();
        }

        /// <summary>
        /// Get generic command templates parameters from script
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
        public virtual IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
        {
            return _commands.GetParametersFromGenericUserScript(connectorClient, userScriptPath);
        }

        /// <summary>
        /// Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Remove direct file transfer access for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKeys">Public keys</param>
        public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys)
        {
            _commands.RemoveDirectFileTransferAccessForUser(connectorClient, publicKeys);
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public virtual void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.CreateJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, hash);
        }
        #region SSH tunnel methods
        /// <summary>
        /// Create tunnel
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Cluster node address</param>
        /// <param name="nodePort">Cluster node port</param>
        public void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
        {
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }


        /// <summary>
        /// Remove tunnel
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task info</param>
        public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }

        /// <summary>
        /// Get tunnels informations
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Cluster node address</param>
        /// <returns></returns>
        public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
        {
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }
        #endregion
        #endregion
        #region Private Methods
        /// <summary>
        /// Get actual tasks info
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="cluster">Cluster</param>
        /// <param name="scheduledJobIds">Scheduled Job ID collection</param>
        /// <returns></returns>
        private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<string> scheduledJobIds)
        {
            var submittedTaskInfos = new List<SubmittedTaskInfo>();
            var scheduledJobIdsList = scheduledJobIds.Select(x => x).Distinct();
            foreach (var jobId in scheduledJobIdsList)
            {
                var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_linuxLocalCommandScripts.GetJobInfoCmdPath} {jobId}/");
                _log.Info($"Get actual task info id=\"{jobId}\", command \"{command}\"");
                submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(cluster, command.Result));
            }
            return submittedTaskInfos;
        }
        #endregion
    }
}

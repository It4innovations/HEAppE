using HEAppE.Exceptions.Internal;
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
        protected readonly CommandScriptPathConfiguration _commandScripts = HPCConnectionFrameworkConfiguration.ScriptsSettings.CommandScriptsPathSettings;

        /// <summary>
        /// Command
        /// </summary>
        protected readonly LinuxLocalCommandScriptPathConfiguration _linuxLocalCommandScripts = HPCConnectionFrameworkConfiguration.ScriptsSettings.LinuxLocalCommandScriptPathSettings;

        /// <summary>
        /// Generic command key parameter
        /// </summary>
        protected static readonly string _genericCommandKeyParameter = HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;

        /// <summary>
        /// Script Configuration
        /// </summary>
        protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

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

            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_scripts.ScriptsBasePath}/{_commandScripts.ExecuteCmdScriptName} {sshCommandBase64}");

            shellCommandSb.Clear();
            string localBasePath = jobSpecification.Cluster.ClusterProjects.Find(cp => cp.ProjectId == jobSpecification.ProjectId)?.LocalBasepath;

            //compose command with parameters of job and task IDs
            shellCommandSb.Append($"{_scripts.ScriptsBasePath}/{_linuxLocalCommandScripts.RunLocalCmdScriptName} {localBasePath}/{HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath}/{jobSpecification.Id}/");
            jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

            //log local HPC Run script to log file
            shellCommandSb.Append($" >> {localBasePath}/{jobSpecification.Id}/job_log.txt");
            shellCommand = shellCommandSb.ToString();

            sshCommandBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand));
            command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{HPCConnectionFrameworkConfiguration.ScriptsSettings.ScriptsBasePath}/run_background_command.sh {sshCommandBase64}");

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

            localClusterJobIds.ToList().ForEach(id => commandSb.Append($"{_scripts.ScriptsBasePath}/{_linuxLocalCommandScripts.CancelJobCmdScriptName} {id};"));
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

            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{ _scripts.SubScriptsPath}/{_linuxLocalCommandScripts.CountJobsCmdScriptName}");
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
        /// <param name="localBasePath"></param>
        /// <param name="sharedAccountsPoolMode"></param>
        public virtual void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
            bool sharedAccountsPoolMode)
        {
            _commands.CreateJobDirectory(connectorClient, jobInfo, localBasePath, sharedAccountsPoolMode);
        }

        /// <summary>
        /// Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo, localBasePath);
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash, string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, localBasePath, hash, path);
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, localBasePath, hash);
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
            throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
        }


        /// <summary>
        /// Remove tunnel
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task info</param>
        public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
        }

        /// <summary>
        /// Get tunnels information
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Cluster node address</param>
        public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
        {
            throw new SchedulerException("NotSupportedEndpoint", nameof(LinuxLocal));
        }

        /// <summary>
        /// Initialize Cluster Script Directory
        /// </summary>
        /// <param name="schedulerConnectionConnection">Connector</param>
        /// <param name="clusterProjectRootDirectory">Cluster project root path</param>
        /// <param name="localBasepath">Cluster execution path</param>
        /// <param name="isServiceAccount">Is servis account</param>
        public void InitializeClusterScriptDirectory(object schedulerConnectionConnection, string clusterProjectRootDirectory, string localBasepath, bool isServiceAccount)
        {
            _commands.InitializeClusterScriptDirectory(schedulerConnectionConnection, clusterProjectRootDirectory, localBasepath, isServiceAccount);
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
                var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_scripts.ScriptsBasePath}/{_linuxLocalCommandScripts.GetJobInfoCmdScriptName} {jobId}/");
                _log.Info($"Get actual task info id=\"{jobId}\", command \"{command}\"");
                submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(cluster, command.Result));
            }
            return submittedTaskInfos;
        }
        #endregion
    }
}

using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.Exceptions;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic
{
    /// <summary>
    /// Slurm scheduler adapter
    /// </summary>
    internal class SlurmSchedulerAdapter : ISchedulerAdapter
    {
        #region Instances
        /// <summary>
        /// Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;

        /// <summary>
        /// Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        /// Logger
        /// </summary>
        protected ILog _log;

        /// <summary>
        /// SSH tunnel
        /// </summary>
        protected static SshTunnelUtils _sshTunnelUtil;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
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
        #region ISchedulerAdapter Members
        /// <summary>
        /// Submit job to scheduler
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobSpecification">Job specification</param>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var schedulerJobIdClusterAllocationNamePairs = new List<(string ScheduledJobId, string ClusterAllocationName)>();
            SshCommandWrapper command = null;

            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "sbatch");
            _log.Info($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");
            string sshCommandBase64 = $"{_commands.InterpreterCommand} '{_commands.ExecuteCmdScriptPath} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64);
                var jobIds = _convertor.GetJobIds(command.Result).ToList();

                for (int i = 0; i < jobSpecification.Tasks.Count; i++)
                {
                    schedulerJobIdClusterAllocationNamePairs.Add((jobIds[i], jobSpecification.Tasks[i].ClusterNodeType.ClusterAllocationName));
                }

                return GetActualTasksInfo(connectorClient, jobSpecification.Cluster, schedulerJobIdClusterAllocationNamePairs);
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when submitting a job: ""{jobSpecification.Name}"" to the cluster: ""{jobSpecification.Cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommandBase64}"".\n", e);
            }
        }

        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="cluster">Cluster</param>
        /// <param name="submitedTasksInfo">Submitted tasks ids</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var submitedTasksInfoList = submitedTasksInfo.ToList();
            try
            {
                return GetActualTasksInfo(connectorClient, cluster, submitedTasksInfoList.Select(s => (s.ScheduledJobId, s.Specification.ClusterNodeType.ClusterAllocationName)));
            }
            catch (SshCommandException)
            {
                _log.Warn($"Scheduled Job ids: \"{string.Join(",", submitedTasksInfoList.Select(s => s.ScheduledJobId))}\" are not in Slurm scheduler database. Mentioned jobs were canceled!");
                return Enumerable.Empty<SubmittedTaskInfo>();
            }
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
        /// <param name="message">Message</param>
        public virtual void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
        {
            StringBuilder cmdBuilder = new();
            foreach (var submitedTaskInfo in submitedTasksInfo)
            {
                var allocationCluster = string.Empty;

                if (!string.IsNullOrEmpty(submitedTaskInfo.Specification.ClusterNodeType.ClusterAllocationName))
                {
                    allocationCluster = $"-M {submitedTaskInfo.Specification.ClusterNodeType.ClusterAllocationName} ";
                }

                cmdBuilder.Append($"{_commands.InterpreterCommand} 'scancel {allocationCluster}{submitedTaskInfo.ScheduledJobId}';");
            }
            string sshCommand = cmdBuilder.ToString();
            _log.Info($"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{sshCommand}\", message \"{message}\"");

            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
        }

        /// <summary>
        /// Get actual scheduler queue status
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="nodeType">Cluster node type</param>
        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            SshCommandWrapper command = null;
            var allocationCluster = string.Empty;

            if (!string.IsNullOrEmpty(nodeType.ClusterAllocationName))
            {
                allocationCluster = $"--clusters={nodeType.ClusterAllocationName} ";
            }

            var sshCommand = $"{_commands.InterpreterCommand} 'sinfo -t alloc {allocationCluster}--partition={nodeType.Queue} -h -o \"%.6D\"'";
            _log.Info($"Get usage of queue \"{nodeType.Queue}\", command \"{sshCommand}\"");

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return _convertor.ReadQueueActualInformation(nodeType, command.Result);
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving usage of Cluster node: ""{nodeType.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for queue usage: ""{sshCommand}""\n", e);
            }
        }

        /// <summary>
        /// Get allocated nodes per task
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task information</param>
        public virtual IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            SshCommandWrapper command = null;
            StringBuilder cmdBuilder = new();

            var cluster = taskInfo.Specification.JobSpecification.Cluster;
            var nodeNames = taskInfo.TaskAllocationNodes.Select(s => $"{s.AllocationNodeId}.{cluster.DomainName ?? cluster.MasterNodeName}")
                                                                    .ToList();

            nodeNames.ForEach(f => cmdBuilder.Append($"dig +short {f};"));

            string sshCommand = cmdBuilder.ToString();
            _log.Info($"Get allocation nodes of task \"{taskInfo.Id}\", command \"{sshCommand}\"");
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return command.Result.Split('\n').Where(w => !string.IsNullOrEmpty(w))
                                                  .ToList(); ;
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving allocation nodes used by running task (HPC job): ""{taskInfo.ScheduledJobId}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
            }
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
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
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
            _sshTunnelUtil.CreateTunnel(connectorClient, taskInfo.Id, nodeHost, nodePort);
        }

        /// <summary>
        /// Remove tunnel
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskInfo">Task info</param>
        public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
        {
            _sshTunnelUtil.RemoveTunnel(connectorClient, taskInfo.Id);
        }

        /// <summary>
        /// Get tunnels informations
        /// </summary>
        /// <param name="taskInfo">Task info</param>
        /// <param name="nodeHost">Cluster node address</param>
        /// <returns></returns>
        public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
        {
            return _sshTunnelUtil.GetTunnelsInformations(taskInfo.Id, nodeHost);
        }
        #endregion
        #endregion
        #region Private Methods
        /// <summary>
        /// Get actual tasks (HPC jobs) informations
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="cluster">Cluster"</param>
        /// <param name="schedulerJobIdClusterAllocationNamePairs">Scheduler job id´s pair</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<(string ScheduledJobId, string ClusterAllocationName)> schedulerJobIdClusterAllocationNamePairs)
        {
            SshCommandWrapper command = null;
            StringBuilder cmdBuilder = new();

            foreach (var (ScheduledJobId, ClusterAllocationName) in schedulerJobIdClusterAllocationNamePairs)
            {
                var allocationCluster = string.Empty;

                if (!string.IsNullOrEmpty(ClusterAllocationName))
                {
                    allocationCluster = $"-M {ClusterAllocationName} ";
                }

                cmdBuilder.Append($"{_commands.InterpreterCommand} 'scontrol show JobId {allocationCluster}{ScheduledJobId} -o';");
            }
            string sshCommand = cmdBuilder.ToString();

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                var submittedTasksInfo = _convertor.ReadParametersFromResponse(cluster, command.Result);
                return submittedTasksInfo;
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving parameters of jobIds: ""{string.Join(", ", schedulerJobIdClusterAllocationNamePairs.Select(s => s.ScheduledJobId).ToList())}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
            }
        }
        #endregion
    }
}

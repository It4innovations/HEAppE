using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        protected static SshTunnel _sshTunnelUtil;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="convertor"></param>
        public SlurmSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            //TODO parse from DI
            _log = LogManager.GetLogger(typeof(SlurmSchedulerAdapter));
            _convertor = convertor;
            _sshTunnelUtil = new SshTunnel();
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        /// <summary>
        /// Submit job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobSpecification">Job specification</param>
        /// <param name="credentials">Cluster credentials</param>
        /// <returns></returns>
        public IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var schedulerJobIdClusterAllocationNamePairs = new List<(string ScheduledJobId, string ClusterAllocationName)>();
            SshCommandWrapper command = null;

            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "sbatch");
            string sshCommandBase64 = $"{_commands.InterpreterCommand} '~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64);
                var jobIds = _convertor.GetJobIds(command.Result).ToList();

                for (int i = 0; i < jobSpecification.Tasks.Count; i++)
                {
                    schedulerJobIdClusterAllocationNamePairs.Add((jobIds[i], jobSpecification.Tasks[i].ClusterNodeType.ClusterAllocationName));
                }

                return GetActualTasksInfo(connectorClient, schedulerJobIdClusterAllocationNamePairs);
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
        /// <param name="submitedTasksInfo">Submitted tasks ids</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var submitedTasksInfoList = submitedTasksInfo.ToList();
            try
            {
                return GetActualTasksInfo(connectorClient, submitedTasksInfoList.Select(s => (s.ScheduledJobId, s.Specification.ClusterNodeType.ClusterAllocationName)));
            }
            catch (SshCommandException ce)
            {
                _log.Warn(ce.Message);

                //Reducing unknown job ids!
                var unMissingJobIdsIndexes = new List<int>();
                int i = 0;
                foreach (Match match in Regex.Matches(ce.Message, @"(?<ErrorMessage>.*)\n", RegexOptions.Compiled))
                {
                    if (match.Success && match.Groups.Count == 2)
                    {
                        string jobErrResponseMessage = match.Groups.GetValueOrDefault("ErrorMessage").Value;
                        if (jobErrResponseMessage != "slurm_load_jobs error: Invalid job id specified")
                        {
                            unMissingJobIdsIndexes.Add(i);
                        }
                        i++;
                    }
                }

                var reducedjobIdsWithJobArrayIndexes = new List<SubmittedTaskInfo>();
                for (i = 0; i < unMissingJobIdsIndexes.Count; i++)
                {
                    var index = unMissingJobIdsIndexes[i];
                    reducedjobIdsWithJobArrayIndexes.Add(submitedTasksInfoList[index]);
                }

                if (!unMissingJobIdsIndexes.Any() || reducedjobIdsWithJobArrayIndexes.Count >= submitedTasksInfo.Count())
                {
                    throw new Exception(ce.Message);
                }

                return GetActualTasksInfo(connectorClient, reducedjobIdsWithJobArrayIndexes);
            }
        }

        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="schedulerJobIdClusterAllocationNamePairs">Submitted tasks ids</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<(string ScheduledJobId, string ClusterAllocationName)> schedulerJobIdClusterAllocationNamePairs)
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

                cmdBuilder.Append($"{ _commands.InterpreterCommand} 'scontrol show JobId {allocationCluster}{ScheduledJobId} -o';");
            }
            string sshCommand = cmdBuilder.ToString();

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                var submittedTasksInfo = _convertor.ReadParametersFromResponse(command.Result);
                return submittedTasksInfo;
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving parameters of jobIds: ""{string.Join(", ", schedulerJobIdClusterAllocationNamePairs.Select(s => s.ScheduledJobId).ToList())}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
            }
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobIds>Scheduled job ids</param>
        /// <param name="message">Message</param>
        public void CancelJob(object connectorClient, IEnumerable<string> scheduledJobIds, string message)
        {
            StringBuilder cmdBuilder = new();
            scheduledJobIds.ToList().ForEach(f => cmdBuilder.Append($"{_commands.InterpreterCommand} 'scancel {f}';"));
            string sshCommand = cmdBuilder.ToString();

            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
        }

        /// <summary>
        /// Get current cluster node usage
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="nodeType">Node type</param>
        public ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            var sshCommand = $"{_commands.InterpreterCommand} 'sinfo -t alloc --partition={nodeType.Queue} -h -o \"%.6D\"'";
            SshCommandWrapper command = null;

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return _convertor.ReadQueueActualInformation(command.Result, nodeType);
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving usage of Cluster node: ""{nodeType.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for queue usage: ""{sshCommand}""\n", e);
            }
        }

        /// <summary>
        /// Get allocated nodes per job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            SshCommandWrapper command = null;
            StringBuilder cmdBuilder = new();
            var nodeNames = jobInfo.Tasks.Where(w => w.State == TaskState.Running)
                                          .SelectMany(s => s.TaskAllocationNodes)
                                          .Select(s => s.AllocationNodeId)
                                          .ToList();

            nodeNames.ForEach(f => cmdBuilder.Append($"dig +short {f};"));
            string sshCommand = cmdBuilder.ToString();

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return command.Result.Split('\n');
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving allocation nodes used by running HPC job: ""{string.Join(", ", jobInfo.Tasks.Select(s => s.ScheduledJobId).ToList())}"". 
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
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);

            foreach (Match match in Regex.Matches(sshCommand.Result, @$"{HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    genericCommandParameters.Add(match.Groups[1].Value.TrimStart());
                }
            }
            return genericCommandParameters;
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
        /// Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Conenctor</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
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
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        /// <summary>
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, hash);
        }
        #region SSH tunnel methods
        /// <summary>
        /// Create SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="localHost">Local host</param>
        /// <param name="localPort">Local port</param>
        /// <param name="loginHost">Login host</param>
        /// <param name="nodeHost">Node host</param>
        /// <param name="nodePort">Node port</param>
        /// <param name="credentials">Credentials</param>
        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            _sshTunnelUtil.CreateSshTunnel(jobId, localHost, localPort, loginHost, nodeHost, nodePort, credentials);
        }

        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            _sshTunnelUtil.RemoveSshTunnel(jobId, nodeHost);
        }

        /// <summary>
        /// Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _sshTunnelUtil.SshTunnelExist(jobId, nodeHost);
        }
        #endregion
        #endregion
    }
}

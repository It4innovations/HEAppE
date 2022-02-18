using System;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using log4net;
using Renci.SshNet;
using System.Collections.Generic;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using System.Text;
using HEAppE.HpcConnectionFramework.SystemCommands;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    internal class PbsProSchedulerAdapter : ISchedulerAdapter
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
        public PbsProSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            //TODO parse from DI
            _log = LogManager.GetLogger(typeof(PbsProSchedulerAdapter));
            _convertor = convertor;
            _sshTunnelUtil = new SshTunnel();
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var jobIdsWithJobArrayIndexes = new List<string>();
            SshCommandWrapper command = null;

            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub");
            string sshCommandBase64 = $"bash -lc '~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64);
                var jobIds = _convertor.GetJobIds(command.Result).ToList();

                for (int i = 0; i < jobSpecification.Tasks.Count; i++)
                {
                    jobIdsWithJobArrayIndexes.AddRange(string.IsNullOrEmpty(jobSpecification.Tasks[i].JobArrays)
                                                        ? new List<string> { jobIds[i] }
                                                        : CombineScheduledJobIdWithJobArrayIndexes(jobIds[i], jobSpecification.Tasks[i].JobArrays));

                }
                return GetActualTasksInfo(connectorClient, jobIdsWithJobArrayIndexes);
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
            var jobIdsWithJobArrayIndexes = submitedTasksInfo.SelectMany(s => string.IsNullOrEmpty(s.Specification.JobArrays) 
                                                                                    ? new List<string>() { s.ScheduledJobId } 
                                                                                    : CombineScheduledJobIdWithJobArrayIndexes(s.ScheduledJobId, s.Specification.JobArrays));
            return GetActualTasksInfo(connectorClient, jobIdsWithJobArrayIndexes);
        }

        /// <summary>
        /// Get actual tasks
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobIds">Scheduler job ids</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<string> scheduledJobIds)
        {
            SshCommandWrapper command = null;
            StringBuilder cmdBuilder = new();

            cmdBuilder.Append($"bash -lc 'qstat -f -x {string.Join(" ", scheduledJobIds)}");
            string sshCommand = cmdBuilder.ToString();

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                var submittedTasksInfo = _convertor.ReadParametersFromResponse(command.Result);
                return submittedTasksInfo;
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving parameters of jobIds: ""{string.Join(", ", scheduledJobIds)}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
            }
        }

        /// <summary>
        /// Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobIds">Scheduled job ids</param>
        /// <param name="message">Message</param>
        public virtual void CancelJob(object connectorClient, IEnumerable<string> scheduledJobIds, string message)
        {
            StringBuilder cmdBuilder = new();
            scheduledJobIds.ToList().ForEach(f => cmdBuilder.Append($"bash -lc 'qdel {f}';"));
            string sshCommand = cmdBuilder.ToString();

            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
        }

        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType)
        {
            SshCommandWrapper command = null;
            string sshCommand = $"bash -lc 'qstat -Q -f {nodeType.Queue}'";

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), sshCommand);
                return _convertor.ReadQueueActualInformation(command.Result, nodeType);
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving parameters of queue: ""{nodeType.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
            }
        }

        /// <summary>
        /// Get allocated nodes per job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            //TODO rewrite
#warning this should use database instead of direct read from file
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"cat {jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/AllocationNodeInfo");
            _log.InfoFormat("Allocated nodes: {0}", sshCommand.Result);
            return null;
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
        #region Private Methods
        private static IEnumerable<string> CombineScheduledJobIdWithJobArrayIndexes(string scheduledJobId, string jobArrayParameter)
        {
            var combJobIdAndJobArrayIndex = new List<string>() { scheduledJobId };
            var jobArraysParameters = Regex.Split(jobArrayParameter, @"\D+").Select(x => int.Parse(x))
                                                                             .ToList();

            int minIndex = jobArraysParameters[0];
            int maxIndex = jobArraysParameters[1];
            int step = jobArraysParameters.Count == 3 ? jobArraysParameters[2] : 1;

            for (int i = minIndex; i <= maxIndex; i += step)
            {
                combJobIdAndJobArrayIndex.Add(scheduledJobId.Replace("[]", $"[{i}]"));
            }

            return combJobIdAndJobArrayIndex;
        }
        #endregion
    }
}
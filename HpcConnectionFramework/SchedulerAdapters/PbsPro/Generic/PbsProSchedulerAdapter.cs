using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
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
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    /// <summary>
    /// PBS Professional scheduler adapter
    /// </summary>
    public class PbsProSchedulerAdapter : ISchedulerAdapter
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

        /// <summary>
        /// Generic commnad key parameter
        /// </summary>
        protected static readonly string _genericCommandKeyParameter = HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="convertor">Convertor</param>
        public PbsProSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _log = LogManager.GetLogger(typeof(PbsProSchedulerAdapter));
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
            var jobIdsWithJobArrayIndexes = new List<string>();
            SshCommandWrapper command = null;

            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub  -koed");
            _log.Info($"Submitting job \"{jobSpecification.Id}\", command \"{sshCommand}\"");
            string sshCommandBase64 = $"{_commands.InterpreterCommand} '{_commands.ExecutieCmdScriptPath} {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";

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
                return GetActualTasksInfo(connectorClient, jobSpecification.Cluster, jobIdsWithJobArrayIndexes);
            }
            catch (FormatException e)
            {
                throw new Exception(@$"Exception thrown when submitting a job: ""{jobSpecification.Name}"" to the cluster: ""{jobSpecification.Cluster.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script error message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommandBase64}"".\n", e);
            }
        }

        /// <summary>
        /// Get actual tasks (HPC jobs) informations
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="cluster">Cluster</param>
        /// <param name="submitedTasksInfo">Submitted tasks id´s</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            IEnumerable<string> jobIdsWithJobArrayIndexes = Enumerable.Empty<string>();
            try
            {
                jobIdsWithJobArrayIndexes = submitedTasksInfo.SelectMany(s => string.IsNullOrEmpty(s.Specification.JobArrays)
                                                                                        ? new List<string>() { s.ScheduledJobId }
                                                                                        : CombineScheduledJobIdWithJobArrayIndexes(s.ScheduledJobId, s.Specification.JobArrays));

                return GetActualTasksInfo(connectorClient, cluster, jobIdsWithJobArrayIndexes);
            }
            catch (SshCommandException ce)
            {
                //Reducing unknown job ids!
                var missingJobIds = new List<string>();
                foreach (Match match in Regex.Matches(ce.Message, @"(?<ErrorMessage>.*)\n", RegexOptions.Compiled))
                {
                    if (match.Success)
                    {
                        string jobErrResponseMessage = match.Groups.GetValueOrDefault("ErrorMessage").Value;

                        missingJobIds.AddRange(Regex.Matches(jobErrResponseMessage, @"(qstat: Unknown Job Id )(?<JobId>(\d*(\[[0-9]*\])*)?(.[a-z-]*[\d]*)?)", RegexOptions.Compiled)
                                                  .Where(w => w.Success && !string.IsNullOrEmpty(w.Groups.GetValueOrDefault("JobId").Value))
                                                  .Select(s => s.Groups.GetValueOrDefault("JobId").Value));
                    }
                }

                _log.Warn($"Scheduled Job ids: \"{missingJobIds}\" are not in PBS Professional scheduler database. Mentioned jobs were canceled!");
                var reducedjobIdsWithJobArrayIndexes = jobIdsWithJobArrayIndexes.Except(missingJobIds);
                if (!missingJobIds.Any() || reducedjobIdsWithJobArrayIndexes.Count() >= jobIdsWithJobArrayIndexes.Count())
                {
                    throw new Exception(ce.Message);
                }

                if (!reducedjobIdsWithJobArrayIndexes.Any())
                {
                    return Enumerable.Empty<SubmittedTaskInfo>();
                }

                return GetActualTasksInfo(connectorClient, cluster, reducedjobIdsWithJobArrayIndexes);
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
            submitedTasksInfo.ToList().ForEach(f => cmdBuilder.Append($"{_commands.InterpreterCommand} 'qdel {f.ScheduledJobId}';"));
            string sshCommand = cmdBuilder.ToString();
            _log.Info($"Cancel jobs \"{string.Join(",", submitedTasksInfo.Select(s => s.ScheduledJobId))}\", command \"{sshCommand}\", message \"{message}\"");

            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
        }

        /// <summary>
        /// Get actual scheduler queue status
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="nodeType">Cluster node type</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            SshCommandWrapper command = null;
            string sshCommand = $"{_commands.InterpreterCommand} 'qstat -Q -f {nodeType.Queue}'";
            _log.Info($"Get usage of queue \"{nodeType.Queue}\", command \"{sshCommand}\"");

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                return _convertor.ReadQueueActualInformation(nodeType, command.Result);
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when retrieving parameters of queue: ""{nodeType.Name}"". 
                                       Submission script result: ""{command.Result}"".\nSubmission script message: ""{command.Error}"".\n
                                       Command line for job submission: ""{sshCommand}""\n", e);
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
                                                  .ToList();
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
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);
            _log.Info($"Get parameters of script \"{userScriptPath}\", command \"{sshCommand}\"");

            foreach (Match match in Regex.Matches(sshCommand.Result, @$"{_genericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    genericCommandParameters.Add(match.Groups[1].Value.TrimStart());
                }
            }
            return genericCommandParameters;
        }

        /// <summary>
        /// Allow direct file transfer access for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job information</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Remove direct file transfer access for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(connectorClient, publicKey);
        }

        /// <summary>
        /// Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
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
        /// Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        /// <param name="hash">Hash</param>  
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        /// <summary>
        /// Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
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
        /// <param name="cluster">Cluster</param>
        /// <param name="scheduledJobIds">Scheduler job id´s</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<string> scheduledJobIds)
        {
            SshCommandWrapper command = null;
            StringBuilder cmdBuilder = new();

            cmdBuilder.Append($"{_commands.InterpreterCommand} 'qstat -f -x {string.Join(" ", scheduledJobIds)}'");
            string sshCommand = cmdBuilder.ToString();

            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommand);
                var submittedTasksInfo = _convertor.ReadParametersFromResponse(cluster, command.Result);
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
        /// Create all scheduled job id´s combinated (jobarray indexes)
        /// </summary>
        /// <param name="scheduledJobId">Scheduled job id</param>
        /// <param name="jobArrayParameter">Jobarray parameter for job</param>
        /// <returns></returns>
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
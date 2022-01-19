using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic
{
    /// <summary>
    /// Class: Slurm scheduler adapter
    /// </summary>
    internal class SlurmSchedulerAdapter : ISchedulerAdapter
    {
        #region Properties
        /// <summary>
        /// Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;

        /// <summary>
        /// Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        /// Log4Net logger
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
            //TODO LinuxCommand and SSH tunnel pass to constructor
            _log = LogManager.GetLogger(typeof(SlurmSchedulerAdapter));
            _convertor = convertor;
            _sshTunnelUtil = new SshTunnel();
            _commands = new LinuxCommands();
        }

        #endregion
        #region ISchedulerAdapter Members
        /// <summary>
        /// Method: Submit job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobSpecification">Job specification</param>
        /// <param name="credentials">Cluster credentials</param>
        /// <returns></returns>
        public SubmittedJobInfo SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            SshCommandWrapper command = null;
            string sshCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "sbatch");
            string sshCommandBase64 = $"{_commands.InterpreterCommand} '~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(sshCommand))}'";
            try
            {
                command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), sshCommandBase64);
                int jobId = SlurmConversionUtils.GetJobIdFromJobCode(command.Result);
                //TODO
                return GetActualJobInfo((SshClient)connectorClient, new string[3] { "1", "2", "3" });
            }
            catch (FormatException e)
            {
                throw new Exception($@"Exception thrown when submitting a job to the cluster {jobSpecification.Cluster.Name}. 
                                       Submission script result: {command.Result}\nCommand line for job submission:\n" + sshCommandBase64, e);
            }
        }

        /// <summary>
        /// Method: Cancel job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="scheduledJobId">Scheduled job id</param>
        /// <param name="message">Message</param>
        public void CancelJob(object connectorClient, string scheduledJobId, string message)
        {
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commands.InterpreterCommand} 'scancel {scheduledJobId}'");
        }

        ///// <summary>
        ///// Method: Get actual job information
        ///// </summary>
        ///// <param name="connectorClient">Connector</param>
        ///// <param name="scheduledJobId">Scheduled job id</param>
        //public SubmittedJobInfo GetActualJobInfo(object connectorClient, string scheduledJobId)
        //{
        //    var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commands.InterpreterCommand} 'scontrol show JobId {scheduledJobId} -o'");
        //    return _convertor.ConvertJobToJobInfo(command.Result);
        //}

        public virtual SubmittedTaskInfo[] GetActualTasksInfo(object scheduler, string[] scheduledJobIds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method: Get current cluster node usage
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="nodeType">Node type</param>
        public ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
        {
            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commands.InterpreterCommand} 'sinfo -t alloc --partition={nodeType.Queue} -h -o \"%.6D\"'");
            int nodesUsed = default;

            if (!string.IsNullOrEmpty(command.Result))
            {
                string nodeUsed = command.Result.Replace("\n", "").Replace(" ", "");
                nodesUsed = int.Parse(nodeUsed);
            }

            return new ClusterNodeUsage
            {
                NodeType = nodeType,
                NodesUsed = nodesUsed,
                Priority = default,
                TotalJobs = default
            };
        }

        /// <summary>
        /// Method: Get allocated nodes per job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job information</param>
        public List<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $@"{_commands.InterpreterCommand} 'scontrol show job {jobInfo.Specification.Id} | grep ' NodeList' | awk -F'=' '{"{{print $2}}"}'");
            return SlurmConversionUtils.GetAllocatedNodes(command.Result);
        }

        /// <summary>
        /// Method: Get generic command templates parameters from script
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">Generic script path</param>
        /// <returns></returns>
        public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
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

        ///// <summary>
        ///// Method: Get actual job information
        ///// </summary>
        ///// <param name="connectorClient">Connector</param>
        ///// <param name="scheduledJobIds">Scheduled job Ids</param>
        //public virtual SubmittedJobInfo[] GetActualJobsInfo(object connectorClient, int[] scheduledJobIds)
        //{
        //    List<SubmittedJobInfo> submittedJobsInfo = new List<SubmittedJobInfo>();
        //    foreach (int scheduledJobId in scheduledJobIds)
        //    {
        //        try
        //        {
        //            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_commands.InterpreterCommand} 'scontrol show JobId {scheduledJobId} -o'");
        //            var submittedJobInfo = _convertor.ConvertJobToJobInfo(command.Result);

        //            if (submittedJobInfo != null)
        //            {
        //                submittedJobsInfo.Add(submittedJobInfo);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            _log.WarnFormat("Unknown Job ID {0} in scontrol output. Setting the job's status to Canceled and retry for remaining jobs.", scheduledJobId);
        //        }
        //    }
        //    return submittedJobsInfo.ToArray();
        //}

        /// <summary>
        /// Method: Allow direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(scheduler, publicKey, jobInfo);
        }

        /// <summary>
        /// Method: Remove direct file transfer acces for user
        /// </summary>
        /// <param name="connectorClient">Conenctor</param>
        /// <param name="publicKey">Public key</param>
        /// <param name="jobInfo">Job info</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(scheduler, publicKey, jobInfo);
        }

        /// <summary>
        /// Method: Create job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            _commands.CreateJobDirectory(scheduler, jobInfo);
        }

        /// <summary>
        /// Method: Delete job directory
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        public void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(scheduler, jobInfo);
        }

        /// <summary>
        /// Method: Copy job data from temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        public void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            _commands.CopyJobDataToTemp(scheduler, jobInfo, hash, path);
        }

        /// <summary>
        /// Method: Copy job data to temp folder
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Job info</param>
        /// <param name="hash">Hash</param>
        /// <param name="path">Path</param>
        public void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(scheduler, jobInfo, hash);
        }

        #region SSH tunnel methods
        /// <summary>
        /// Method: Create SSH tunnel
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
        /// Method: Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            _sshTunnelUtil.RemoveSshTunnel(jobId, nodeHost);
        }

        /// <summary>
        /// Method: Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _sshTunnelUtil.SshTunnelExist(jobId, nodeHost);
        }

        public SubmittedJobInfo GetActualJobInfo(object scheduler, string[] scheduledJobIds)
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion
    }
}

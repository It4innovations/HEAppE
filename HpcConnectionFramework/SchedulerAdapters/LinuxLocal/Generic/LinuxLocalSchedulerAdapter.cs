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
        /// <param name="convertor"></param>
        public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _convertor = convertor;
            _log = LogManager.GetLogger(typeof(LinuxLocalSchedulerAdapter));
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        /// <summary>
        /// Get pararameters defined in generic user script on cluster
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="userScriptPath">User sctipt path</param>
        /// <returns></returns>
        public virtual IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
        {
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommand);

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
            if (int.TryParse(command.Result, out int totalJobs))
            {
                usage.TotalJobs = totalJobs;
            }

            return usage;
        }

        /// <summary>
        /// Allow Direct File Transfer Access for user to job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public Key (RSA)</param>
        /// <param name="jobInfo">Submitted JobInfo</param>
        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        /// <summary>
        /// Remove Direct File Transfer Access for user to job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="publicKey">Public Key (RSA)</param>
        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(connectorClient, publicKey);
        }

        /// <summary>
        /// Create job directory whith task subdirectories
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Submitted JobInfo</param>
        public virtual void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.CreateJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Submit job to cluster
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobSpecification">Job Specification</param>
        /// <param name="credentials">Cluster Authentication Credentials</param>
        /// <returns></returns>
        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var shellCommandSb = new StringBuilder();

            string shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"{_commandScripts.ExecutieCmdPath} {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            _log.InfoFormat("Run prepare-job result: {0}", sshCommand.Result);

            shellCommandSb.Clear();

            //compose command with parameters of job and task IDs
            shellCommandSb.Append($"{_linuxLocalCommandScripts.RunLocalCmdPath} {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/" +
                $"{jobSpecification.Id}/");
            jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

            //log local HPC Run script to log file
            shellCommandSb.Append($" >> {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/job_log.txt &");

            shellCommand = shellCommandSb.ToString();

            sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"{_commandScripts.ExecutieCmdPath} {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");

            return GetActualTasksInfo(connectorClient, jobSpecification.Cluster, new string[] { $"{jobSpecification.Id}" });
        }

        /// <summary>
        /// Get actual tasks info
        /// </summary>
        /// <param name="connectorClient"><Connector/param>
        /// <param name="cluster">Cluster</param>
        /// <param name="submitedTasksInfo">SubmittedTask collection</param>
        /// <returns></returns>
        public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var localClusterJobIds = submitedTasksInfo
                .Select(s => s.Specification.JobSpecification.Id.ToString())
                .Distinct();
            return GetActualTasksInfo(connectorClient, cluster, localClusterJobIds);
        }


        /// <summary>
        /// Cancel Job
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="submitedTasksInfo">Submitted TaskInfo collection</param>
        /// <param name="message">Message</param>
        public virtual void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
        {
            StringBuilder commandSb = new();
            var localClusterJobIds = submitedTasksInfo
                .Select(s => s.Specification.JobSpecification.Id.ToString())
                .Distinct();
            localClusterJobIds.ToList().ForEach(id => commandSb.Append($"{_linuxLocalCommandScripts.CancelJobCmdPath} {id};"));
            string command = commandSb.ToString();
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), command);
        }

        /// <summary>
        /// Delete Job Directory recursively
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Submitted JonInfo</param>
        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo);
        }

        /// <summary>
        /// Copy Job Data To Temporary storage
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Submitted JonInfo</param>
        /// <param name="hash">SessionCode Hash</param>
        /// <param name="path">Path</param>
        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        /// <summary>
        /// Copy Job Data From Temporary storage
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Submitted JonInfo</param>
        /// <param name="hash">SessionCode Hash</param>
        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, hash);
        }

        /// <summary>
        /// Get Allocated Nodes
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="jobInfo">Submitted JobInfo</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            List<string> allocatedNodes = new();
            StringBuilder allocationNodeSb = new ();
            foreach (var task in jobInfo.Tasks)
            {
                allocationNodeSb.Clear();
                string domainName = task.Specification.ClusterNodeType.Cluster.DomainName;
                if (string.IsNullOrEmpty(domainName))
                {
                    domainName = this.LocalDomainName;
                    
                }
                allocationNodeSb.Append(domainName);
                if (task.NodeType.Cluster.Port.HasValue)
                {
                    allocationNodeSb.Append($":{task.NodeType.Cluster.Port.Value}");
                }
                allocatedNodes.Add(allocationNodeSb.ToString());
            }
            return allocatedNodes.Distinct();
        }

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
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }
        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }
        /// <summary>
        /// Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            throw new Exception($"{nameof(LinuxLocal)} Scheduler does not suport this endpoint.");
        }
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
                submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(cluster, command.Result));
            }
            return submittedTaskInfos;
        }
        #endregion
    }
}

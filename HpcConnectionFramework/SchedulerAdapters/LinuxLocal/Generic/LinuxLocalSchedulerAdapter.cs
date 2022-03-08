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
        /// Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        /// Command
        /// </summary>
        protected readonly LinuxLocalCommandScriptPathConfiguration _linuxLocalCommandScripts = HPCConnectionFrameworkConfiguration.LinuxLocalCommandScriptPathSettings;

        /// <summary>
        /// Generic commnad key parameter
        /// </summary>
        protected static readonly string _genericCommandKeyParameter = HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter;
        #endregion
        #region Constructors
        public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _convertor = convertor;
            _log = LogManager.GetLogger(typeof(LinuxLocalSchedulerAdapter));
            _commands = new LinuxCommands();
        }
        #endregion
        #region ISchedulerAdapter Members
        private IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<string> scheduledJobIds)
        {
            var submittedTaskInfos = new List<SubmittedTaskInfo>();
            foreach (var jobId in scheduledJobIds.Select(x => x).Distinct())
            {
                var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), $"{_linuxLocalCommandScripts.GetJobInfoCmdPath} {jobId}/");
                submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(cluster, command.Result));
            }
            return submittedTaskInfos;
        }

        public virtual IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            return GetActualTasksInfo(connectorClient, cluster, submitedTasksInfo.Select(s => s.ScheduledJobId));
        }

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

        public virtual IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedJobInfo jobInfo)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            var shellCommandSb = new StringBuilder();
            var jobResultInfo = new StringBuilder();

            #region Prepare Job Directory
            string shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            jobResultInfo.Append(sshCommand.Result);
            _log.InfoFormat("Run prepare-job result: {0}", jobResultInfo.ToString());

            shellCommandSb.Clear();
            #endregion

            #region Compose Local run script
            shellCommandSb.Append($"{_linuxLocalCommandScripts.RunLocalCmdPath} {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/");//run job (script on local linux docker machine)
            jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

            shellCommandSb.Append($" >> {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/job_log.txt &");//change this in future?

            shellCommand = shellCommandSb.ToString();

            sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            #endregion
            return GetActualTasksInfo(connectorClient, jobSpecification.Cluster, new string[] { $"{jobSpecification.Id}" });
        }

        public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(connectorClient, publicKey, jobInfo);
        }

        public void RemoveDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(connectorClient, publicKey);
        }

        #region JobManagement
        public virtual void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            StringBuilder shellCommandSb = new StringBuilder();
            shellCommandSb.Append($"~/.key_scripts/create_job_directory.sh {jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id};");
            jobInfo.Tasks.ForEach(task => shellCommandSb.Append($"~/.key_scripts/create_job_directory.sh {jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{task.Specification.Id};"));

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), shellCommandSb.ToString());
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        public virtual void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
        {
            StringBuilder commandSb = new();
            submitedTasksInfo.ToList().ForEach(f => commandSb.Append($"{_linuxLocalCommandScripts.CancelJobCmdPath} {f.ScheduledJobId};"));
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)connectorClient), commandSb.ToString());
        }

        public void DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(connectorClient, jobInfo);
        }

        public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            _commands.CopyJobDataToTemp(connectorClient, jobInfo, hash, path);
        }

        public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(connectorClient, jobInfo, hash);
        }
        #endregion
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

        public void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            throw new NotImplementedException();
        }

        public void RemoveSshTunnel(long jobId, string nodeHost)
        {
            throw new NotImplementedException();
        }

        public bool SshTunnelExist(long jobId, string nodeHost)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
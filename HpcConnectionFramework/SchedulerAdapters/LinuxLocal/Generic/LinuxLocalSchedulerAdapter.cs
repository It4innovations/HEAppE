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
        /// Commands
        /// </summary>
        protected ICommands _commands;

        /// <summary>
        ///   Convertor reference.
        /// </summary>
        protected ISchedulerDataConvertor _convertor;
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
        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object scheduler, IEnumerable<string> scheduledJobIds)
        {
            var submittedTaskInfos = new List<SubmittedTaskInfo>();
            foreach (var jobId in scheduledJobIds.Select(x => x).Distinct())
            {
                var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"{LinuxLocalCommandScriptPathConfiguration.GetJobInfoCmdPath} {jobId}/");
                submittedTaskInfos.AddRange(_convertor.ReadParametersFromResponse(command.Result));
            }
            return submittedTaskInfos;
        }

        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            return GetActualTasksInfo(connectorClient, submitedTasksInfo.Select(s => s.ScheduledJobId));
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType)
        {
            ClusterNodeUsage usage = new ClusterNodeUsage
            {
                NodeType = nodeType
            };

            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), LinuxLocalCommandScriptPathConfiguration.CountJobsCmdPath);
            if (int.TryParse(command.Result, out int totalJobs))
            {
                usage.TotalJobs = totalJobs;
            }

            return usage;
        }

        public IEnumerable<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SubmittedTaskInfo> SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            StringBuilder shellCommandSb = new StringBuilder();
            StringBuilder jobResultInfo = new StringBuilder();

            #region Prepare Job Directory
            string shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            jobResultInfo.Append(sshCommand.Result);
            _log.InfoFormat("Run prepare-job result: {0}", jobResultInfo.ToString());

            shellCommandSb.Clear();
            #endregion

            #region Compose Local run script
            shellCommandSb.Append($"{LinuxLocalCommandScriptPathConfiguration.RunLocalCmdPath} " +
                $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/");//run job (script on local linux docker machine)
            jobSpecification.Tasks.ForEach(task => shellCommandSb.Append($" {task.Id}"));

            shellCommandSb.Append($" >> {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/job_log.txt &");//change this in future?

            shellCommand = shellCommandSb.ToString();

            sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            #endregion
            return GetActualTasksInfo(scheduler, new string[] { $"{jobSpecification.Id}" });
        }

        public void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.AllowDirectFileTransferAccessForUserToJob(scheduler, publicKey, jobInfo);
        }

        public void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            _commands.RemoveDirectFileTransferAccessForUserToJob(scheduler, publicKey, jobInfo);
        }

        #region JobManagement

        public void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            StringBuilder shellCommandSb = new StringBuilder();
            shellCommandSb.Append($"~/.key_scripts/create_job_directory.sh {jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id};");
            jobInfo.Tasks.ForEach(task => shellCommandSb.Append($"~/.key_scripts/create_job_directory.sh " +
                $"{jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath}/{jobInfo.Specification.Id}/{task.Specification.Id};"));

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommandSb.ToString());
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        public void CancelJob(object scheduler, IEnumerable<string> scheduledJobIds, string message)
        {
            StringBuilder commandSb = new();
            scheduledJobIds.ToList().ForEach(scheduledJobId => commandSb.Append($"{LinuxLocalCommandScriptPathConfiguration.CancelJobCmdPath} {scheduledJobId};"));
            SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), commandSb.ToString());
        }

        public void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            _commands.DeleteJobDirectory(scheduler, jobInfo);
        }

        public void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            _commands.CopyJobDataToTemp(scheduler, jobInfo, hash, path);
        }

        public void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            _commands.CopyJobDataFromTemp(scheduler, jobInfo, hash);
        }

        #endregion

        public IEnumerable<string> GetParametersFromGenericUserScript(object scheduler, string userScriptPath)
        {
            var genericCommandParameters = new List<string>();
            string shellCommand = $"cat {userScriptPath}";
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);

            foreach (Match match in Regex.Matches(sshCommand.Result,
                @$"{HPCConnectionFrameworkConfiguration.GenericCommandKeyParameter}([\s\t]+[A-z_\-]+)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
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
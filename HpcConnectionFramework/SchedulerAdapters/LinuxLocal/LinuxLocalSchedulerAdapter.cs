using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using Renci.SshNet;
using log4net;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
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
        #endregion
        #region Constructors
        public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor)
        {
            _convertor = convertor;
            _log = LogManager.GetLogger(typeof(LinuxLocalSchedulerAdapter));
        }
        #endregion
        #region ISchedulerAdapter Members
        private SubmittedJobInfo GetActualJobInfo(object scheduler, string pathToJobInfo)
        {
            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"~/.local_hpc_scripts/get_job_info.sh {pathToJobInfo}");
            return null;// _convertor.ConvertJobToJobInfo(command.Result);//todo
        }

        public IEnumerable<SubmittedTaskInfo> SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder jobResultInfo = new StringBuilder();

            #region Prepare Job Directory
            string shellCommand = (string)_convertor.ConvertJobSpecificationToJob(jobSpecification, null);

            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            jobResultInfo.Append(sshCommand.Result);
            _log.InfoFormat("Run prepare-job result: {0}", jobResultInfo.ToString());

            sb.Clear();
            #endregion

            #region Compose Local run script
            sb.Append($"~/.local_hpc_scripts/run_local.sh " +
                $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/");//run job (script on local linux docker machine)
            foreach (var task in jobSpecification.Tasks)
            {
                sb.Append($" {task.Id}");
            }

            sb.Append($" >> {jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/job_log.txt &");//change this in future?

            shellCommand = sb.ToString();

            sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler),
                $"~/.key_scripts/run_command.sh {Convert.ToBase64String(Encoding.UTF8.GetBytes(shellCommand))}");
            #endregion
            return null;
            //return GetActualJobInfo(scheduler, $"{jobSpecification.Id}/");
        }


        public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo)
        {
            var submittedTaskInfos = new List<SubmittedTaskInfo>();
            //TODO
            //foreach (var jobId in scheduledJobIds.Select(x => x.Substring(0, x.IndexOf('.'))).Distinct())
            //{
            //    submittedTaskInfos.AddRange(GetActualJobInfo(scheduler, $"{jobId}/").Tasks);
            //}
            return submittedTaskInfos;
        }

        public void CancelJob(object scheduler, IEnumerable<string> scheduledJobIds, string message)
        {
            //TODO
            //scheduledJobId = scheduledJobId.Substring(0, scheduledJobId.IndexOf('.'));
            //var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"~/.local_hpc_scripts/cancel_job.sh {scheduledJobId}/");
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(object scheduler, ClusterNodeType nodeType)
        {
            ClusterNodeUsage usage = new ClusterNodeUsage
            {
                NodeType = nodeType
            };

            var command = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"~/.local_hpc_scripts/count_jobs.sh");
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

        public void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/add_key.sh {0} {1}", publicKey, jobInfo.Specification.Id);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Allow file transfer result: {0}", sshCommand.Result));
        }

        public void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/remove_key.sh {0}", publicKey);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Remove permission for direct file transfer result: {0}", sshCommand.Result));
        }

        public void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("~/.key_scripts/create_job_directory.sh {0}/{1};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id));
            foreach (var task in jobInfo.Tasks)
            {
                sb.Append(String.Format("~/.key_scripts/create_job_directory.sh {0}/{1}/{2};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id, task.Specification.Id));
            }

            string shellCommand = sb.ToString();
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        public void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            string shellCommand = String.Format("rm -Rf {0}/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job directory {0} was deleted", jobInfo.Specification.Id);
        }

        public void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = String.Format("{0}/{1}/{2}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id, path);
            string outputDirectory = String.Format("{0}Temp/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, hash);
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;//copy just content
            string shellCommand = String.Format("~/.key_scripts/copy_data_to_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job data {0}/{1} were copied to temp directory {2}, result: {3}", jobInfo.Specification.Id, path, hash, sshCommand.Result);
        }

        public void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = String.Format("{0}Temp/{1}/.", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, hash);
            string outputDirectory = String.Format("{0}/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            string shellCommand = String.Format("~/.key_scripts/copy_data_from_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = SshCommandUtils.RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Temp data {0} were copied to job directory {1}, result: {2}", hash, jobInfo.Specification.Id, sshCommand.Result);
        }

        public IEnumerable<string> GetParametersFromGenericUserScript(object scheduler, string userScriptPath)
        {
            throw new NotImplementedException();
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
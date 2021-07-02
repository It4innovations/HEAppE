using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.LinuxPbs;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10;
using HEAppE.HpcConnectionFramework.LinuxPbs.v12;
using HEAppE.MiddlewareUtils;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
{
    public class LinuxLocalSchedulerAdapter : LinuxPbsV12SchedulerAdapter
    {
        private readonly string WorkDirBasePath = "/home/heappetests";//REFLECTING DOCKERFILE FOR LOCAL LINUX COMPUTING
        #region Constructors
        public LinuxLocalSchedulerAdapter(ISchedulerDataConvertor convertor) : base(convertor) { }
        #endregion

        #region ISchedulerAdapter Members
        public override SubmittedJobInfo GetActualJobInfo(object scheduler, string pathToJobInfo)
        {
            var command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"~/.key_script/get_job_info.sh {pathToJobInfo}");
            return _convertor.ConvertJobToJobInfo(command.Result);//todo
        }

        public override SubmittedJobInfo SubmitJob(object scheduler, JobSpecification jobSpecification,
            ClusterAuthenticationCredentials credentials)
        {
            var localHpcJobInfo =
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jobSpecification.ConvertToLocalHPCInfo()));
            StringBuilder sb = new StringBuilder();
            StringBuilder jobResultInfo = new StringBuilder();

            //preparation script, prepares job info file to the job directory at local linux "cluster"
            sb.Append(
                $"~/.key_script/prepare_job_dir.sh " +
                $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/ {localHpcJobInfo};");

            var shellCommand = sb.ToString();
            var sshCommand =  RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            jobResultInfo.Append(sshCommand.Result);
            _log.InfoFormat("Run prepare-job result: {0}", jobResultInfo.ToString());

            sb.Clear();

            sb.Append($"~/.key_script/run_test.sh {WorkDirBasePath}/{jobSpecification.Id}/");//run job (script on local linux docker machine)
            foreach (var task in jobSpecification.Tasks)
            {
                sb.Append($" {task.Id}");
            }

            sb.Append(";");

            shellCommand = sb.ToString();
             _ = Task.Run( () => RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand));//do not wait for the end

            /*jobResultInfo.Append(sshCommand.Result);
            _log.InfoFormat("Run job result: {0}", jobResultInfo.ToString());*/

            Thread.Sleep(500);//wait till script starts (at docker container) refactor this?

            return GetActualJobInfo(scheduler, $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/");
        }

        public override SubmittedJobInfo[] GetActualJobsInfo(object scheduler, int[] scheduledJobIds)
        {
            var submittedTaskInfos = new List<SubmittedJobInfo>();

            foreach (var jobId in scheduledJobIds)
            {
                submittedTaskInfos.Add(GetActualJobInfo(scheduler, $"{WorkDirBasePath}/{jobId}/"));
            }

            return submittedTaskInfos.ToArray();
        }


        public override SubmittedTaskInfo[] GetActualTasksInfo(object scheduler, string[] scheduledJobIds)
        {
            var submittedTaskInfos = new List<SubmittedTaskInfo>();

            foreach (var jobId in scheduledJobIds)
            {
                submittedTaskInfos.AddRange(GetActualJobInfo(scheduler, $"{WorkDirBasePath}/{jobId}/").Tasks);
            }

            return submittedTaskInfos.ToArray();
        }

        public override SubmittedJobInfo GetActualJobInfo(object scheduler, string[] scheduledJobIds)
        {
            SubmittedJobInfo jobInfo = new SubmittedJobInfo();
            jobInfo.Tasks = new List<SubmittedTaskInfo>(GetActualTasksInfo(scheduler, scheduledJobIds));
            return jobInfo;
        }

        public override void CancelJob(object scheduler, string scheduledJobId, string message)
        {
            var command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), $"~/.key_script/cancel_job.sh {WorkDirBasePath}/{scheduledJobId}/");
            //throw new NotImplementedException("todo");
        }

        public override List<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo)
        {
#warning this should use database instead of direct read from file
            string shellCommand = String.Format("cat {0}/{1}/nodefile", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Allocated nodes: {0}", sshCommand.Result);
            return LinuxPbsConversionUtils.ConvertNodesUrlsToList(sshCommand.Result);
        }

        public override void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_script/add_key.sh {0} {1}", publicKey, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Allow file transfer result: {0}", sshCommand.Result));
        }

        public override void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_script/remove_key.sh {0}", publicKey);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Remove permission for direct file transfer result: {0}", sshCommand.Result));
        }

        public override void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("~/.key_script/create_job_directory.sh {0}/{1};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id));
            foreach (var task in jobInfo.Tasks)
            {
                sb.Append(String.Format("~/.key_script/create_job_directory.sh {0}/{1}/{2};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id, task.Specification.Id));
            }

            string shellCommand = sb.ToString();
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Create job directory result: {0}", sshCommand.Result);
        }

        public override void DeleteJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            string shellCommand = String.Format("rm -Rf {0}/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job directory {0} was deleted", jobInfo.Specification.Id);
        }

        public override void CopyJobDataToTemp(object scheduler, SubmittedJobInfo jobInfo, string hash, string path)
        {
            //if path is null or empty then all files and directories from ClusterLocalBasepath will be copied to hash directory
            string inputDirectory = String.Format("{0}/{1}/{2}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id, path);
            string outputDirectory = String.Format("{0}Temp/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, hash);
            inputDirectory += string.IsNullOrEmpty(path) ? "." : string.Empty;//copy just content
            string shellCommand = String.Format("~/.key_script/copy_data_to_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job data {0}/{1} were copied to temp directory {2}, result: {3}", jobInfo.Specification.Id, path, hash, sshCommand.Result);
        }

        public override void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = String.Format("{0}Temp/{1}/.", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, hash);
            string outputDirectory = String.Format("{0}/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            string shellCommand = String.Format("~/.key_script/copy_data_from_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Temp data {0} were copied to job directory {1}, result: {2}", hash, jobInfo.Specification.Id, sshCommand.Result);
        }
        #endregion
    }
}
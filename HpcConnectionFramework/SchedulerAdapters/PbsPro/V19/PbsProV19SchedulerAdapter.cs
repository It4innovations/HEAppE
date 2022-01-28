using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using Renci.SshNet;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19
{
    public class PbsProV19SchedulerAdapter : PbsProSchedulerAdapter
    {
        #region Constructors
        public PbsProV19SchedulerAdapter(ISchedulerDataConvertor convertor) : base(convertor) { }
        #endregion

        #region ISchedulerAdapter Members
        //public override SubmittedJobInfo GetActualJobInfo(object scheduler, string scheduledJobId)
        //{
        //    var command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), String.Format("bash -lc 'qstat -f -x {0}'", scheduledJobId));
        //    return _convertor.ConvertJobToJobInfo(command.Result);
        //}

        //public override SubmittedJobInfo[] GetActualJobsInfo(object scheduler, int[] scheduledJobIds)
        //{
        //    const string JOB_ID_REGEX = @"^(Job\ Id:\ )(\d+)";

        //    SshCommandWrapper command = null;
        //    do
        //    {
        //        string commandString = String.Format("bash -lc 'qstat -f -x {0}'", String.Join(" ", scheduledJobIds));
        //        try
        //        {
        //            command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), commandString);
        //        }
        //        catch (Exception e)
        //        {
        //            string jobIdMath = "qstat: Unknown Job Id";
        //            if (e.Message.Contains(jobIdMath))
        //            {
        //                Match match = Regex.Match(e.Message, @"(?<=Unknown Job Id )\b\w+\b");
        //                if (match.Success)
        //                {
        //                    int jobId = Convert.ToInt32(match.Value);
        //                    _log.WarnFormat("Unknown Job ID {0} in qstat output. Setting the job's status to Canceled and retry for remaining jobs.", jobId);
        //                    scheduledJobIds = scheduledJobIds.Where(val => val != jobId).ToArray();
        //                    command = null;
        //                }
        //            }
        //            else _log.ErrorFormat(e.Message);
        //        }
        //    }
        //    while (command == null);

        //    string[] resultLines = command.Result.Split('\n');

        //    // Search for lines with jobIds
        //    Dictionary<int, int> jobLines = new Dictionary<int, int>();
        //    for (int i = 0; i < resultLines.Length; i++)
        //    {
        //        Match match = Regex.Match(resultLines[i], JOB_ID_REGEX);
        //        if (match.Success)
        //        {
        //            jobLines.Add(Convert.ToInt32(match.Groups[2].Value), i);
        //        }
        //    }

        //    // Iterate through jobIds and extract job info
        //    SubmittedJobInfo[] result = new SubmittedJobInfo[scheduledJobIds.Length];
        //    for (int i = 0; i < scheduledJobIds.Length; i++)
        //    {
        //        // Search for jobId in result
        //        int jobInfoStartLine = -1;
        //        try
        //        {
        //            jobInfoStartLine = jobLines[scheduledJobIds[i]];
        //        }
        //        catch (KeyNotFoundException)
        //        {
        //            _log.ErrorFormat("Job ID {0} not found in qstat output.", scheduledJobIds[i]);
        //            continue;
        //        }

        //        // Get number of lines in qstat output for this job
        //        int jobInfoLineCount = 0;
        //        do
        //        {
        //            jobInfoLineCount++;
        //        } while (!Regex.IsMatch(resultLines[jobInfoStartLine + jobInfoLineCount], JOB_ID_REGEX) && jobInfoLineCount + jobInfoStartLine + 1 < resultLines.Length);

        //        // Cut lines for given job info
        //        string[] currentJobLines = new string[jobInfoLineCount];
        //        Array.Copy(resultLines, jobInfoStartLine, currentJobLines, 0, jobInfoLineCount);

        //        // Get current job info
        //        result[i] = _convertor.ConvertJobToJobInfo(String.Join("\n", currentJobLines));
        //    }
        //    return result;
        //}

        public override IEnumerable<SubmittedTaskInfo> SubmitJob(object scheduler, JobSpecification jobSpecification, ClusterAuthenticationCredentials credentials)
        {
            SshCommandWrapper command = null;
            var qsubTaskCommandBytes = System.Text.Encoding.UTF8.GetBytes((string)_convertor.ConvertJobSpecificationToJob(jobSpecification, "qsub"));
            string job = "bash -lc '~/.key_scripts/run_command.sh " + Convert.ToBase64String(qsubTaskCommandBytes) + "'";
            try
            {
                command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), job);

                string[] jobIds = PbsProConversionUtils.GetJobIdsFromJobCode(command.Result);
                if (jobIds == null || jobIds.Length == 0)
                    throw new Exception("Exception thrown when submitting a job to the cluster. Submission script result: " + command.Result + "\nError: " + command.Error + "\nCommand line for job submission:\n" + job);

                return null;// GetActualJobInfo(scheduler, jobIds);
            }
            catch (FormatException e)
            {
                throw new Exception(
                    "Exception thrown when submitting a job to the cluster. Submission script result: " + command.Result +
                    "\nCommand line for job submission:\n" + job, e);
            }
        }



        public override IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object scheduler, IEnumerable<string> scheduledJobIds)
        {
            const string JOB_ID_REGEX = @"^(Job\ Id:\ )([a-zA-Z0-9\.\[\]\-]+)";
            var scheduledJobIdsArray = scheduledJobIds.ToArray();
            SshCommandWrapper command = null;
            do
            {
                string commandString = $"bash -lc 'qstat -f -x {string.Join(" ", scheduledJobIdsArray)}'";
                try
                {
                    command = RunSshCommand(new SshClientAdapter((SshClient)scheduler), commandString);
                }
                catch (SshCommandException ce)
                {
                    _log.Warn(ce.Message);
                    break;
                }
                catch (Exception e)
                {
                    string jobIdMath = "qstat: Unknown Job Id";
                    if (e.Message.Contains(jobIdMath))
                    {
                        Match match = Regex.Match(e.Message, @$"(?<={jobIdMath} )\b\w+[.\w]*\b", RegexOptions.Compiled);
                        if (match.Success)
                        {
                            string jobId = match.Value;
                            _log.Warn($"Unknown Job ID {jobId} in qstat output. Setting the job's status to Canceled and retry for remaining jobs.");
                            scheduledJobIdsArray = scheduledJobIdsArray?.Where(val => val != jobId)
                                                               .ToArray();
                            command = null;
                        }
                    }
                    else
                    {
                        _log.ErrorFormat(e.Message);
                    }
                }
            }
            while (command == null);


            if (command is not null)
            {
                string[] resultLines = command.Result.Split('\n');

                // Search for lines with jobIds
                Dictionary<string, int> jobLines = new Dictionary<string, int>();
                for (int i = 0; i < resultLines.Length; i++)
                {
                    Match match = Regex.Match(resultLines[i], JOB_ID_REGEX);
                    if (match.Success)
                    {
                        jobLines.Add(match.Groups[2].Value, i);
                    }
                }

                // Iterate through jobIds and extract task info
                List<SubmittedTaskInfo> taskInfos = new List<SubmittedTaskInfo>();
                for (int i = 0; i < scheduledJobIdsArray?.Length; i++)
                {
                    // Search for jobId in result
                    int jobInfoStartLine = -1;
                    try
                    {
                        jobInfoStartLine = jobLines[scheduledJobIdsArray[i]];
                    }
                    catch (KeyNotFoundException)
                    {
                        _log.ErrorFormat("Job ID {0} not found in qstat output.", scheduledJobIdsArray[i]);

                        taskInfos.Add(new SubmittedTaskInfo());
                        continue;
                    }

                    // Get number of lines in qstat output for this job
                    int jobInfoLineCount = 0;
                    do
                    {
                        jobInfoLineCount++;
                    } while (!Regex.IsMatch(resultLines[jobInfoStartLine + jobInfoLineCount], JOB_ID_REGEX) && jobInfoLineCount + jobInfoStartLine + 1 < resultLines.Length);

                    // Cut lines for given job info
                    string[] currentJobLines = new string[jobInfoLineCount];
                    Array.Copy(resultLines, jobInfoStartLine, currentJobLines, 0, jobInfoLineCount);

                    // Get current job info
                    //TODO
                    //taskInfos.Add(_convertor.ConvertTaskToTaskInfo(String.Join("\n", currentJobLines)));
                }
                return taskInfos;
            }
            else
            {
                return Enumerable.Empty<SubmittedTaskInfo>();
            }
        }

        private SubmittedJobInfo GetActualJobInfo(object scheduler, string[] scheduledJobIds)
        {
            SubmittedJobInfo jobInfo = new SubmittedJobInfo();
            jobInfo.Tasks = new List<SubmittedTaskInfo>(GetActualTasksInfo(scheduler, scheduledJobIds));
            return jobInfo;
        }

        public override List<string> GetAllocatedNodes(object scheduler, SubmittedJobInfo jobInfo)
        {
#warning this should use database instead of direct read from file
            string shellCommand = String.Format("cat {0}/{1}/AllocationNodeInfo", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Allocated nodes: {0}", sshCommand.Result);
            return PbsProConversionUtils.ConvertNodesUrlsToList(sshCommand.Result);
        }

        public override void AllowDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/add_key.sh {0} {1}", publicKey, jobInfo.Specification.Id);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Allow file transfer result: {0}", sshCommand.Result));
        }

        public override void RemoveDirectFileTransferAccessForUserToJob(object scheduler, string publicKey, SubmittedJobInfo jobInfo)
        {
            publicKey = StringUtils.RemoveWhitespace(publicKey);
            string shellCommand = String.Format("~/.key_scripts/remove_key.sh {0}", publicKey);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.Info(String.Format("Remove permission for direct file transfer result: {0}", sshCommand.Result));
        }

        public override void CreateJobDirectory(object scheduler, SubmittedJobInfo jobInfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("~/.key_scripts/create_job_directory.sh {0}/{1};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id));
            foreach (var task in jobInfo.Tasks)
            {
                sb.Append(String.Format("~/.key_scripts/create_job_directory.sh {0}/{1}/{2};", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id, task.Specification.Id));
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
            string shellCommand = String.Format("~/.key_scripts/copy_data_to_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Job data {0}/{1} were copied to temp directory {2}, result: {3}", jobInfo.Specification.Id, path, hash, sshCommand.Result);
        }

        public override void CopyJobDataFromTemp(object scheduler, SubmittedJobInfo jobInfo, string hash)
        {
            string inputDirectory = String.Format("{0}Temp/{1}/.", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, hash);
            string outputDirectory = String.Format("{0}/{1}", jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification.Id);
            string shellCommand = String.Format("~/.key_scripts/copy_data_from_temp.sh {0} {1}", inputDirectory, outputDirectory);
            var sshCommand = RunSshCommand(new SshClientAdapter((SshClient)scheduler), shellCommand);
            _log.InfoFormat("Temp data {0} were copied to job directory {1}, result: {2}", hash, jobInfo.Specification.Id, sshCommand.Result);
        }
        #endregion
    }
}
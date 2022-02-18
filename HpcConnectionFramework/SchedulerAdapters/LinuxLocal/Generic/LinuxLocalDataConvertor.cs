using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;
using HEAppE.MiddlewareUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal
{
    public class LinuxLocalDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        public LinuxLocalDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) { }
        public LinuxLocalDataConvertor() : base(null) { }
        #endregion
        #region SchedulerDataConvertor Members
        private List<SubmittedTaskInfo> ConvertTasksToTaskInfoCollection(LinuxLocalInfo jobInfo, List<LinuxLocalJobDTO> allTasks)
        {
            List<SubmittedTaskInfo> taskCollection = new();

            foreach (var taskAdapter in allTasks)
            {
                taskAdapter.CreationTime = jobInfo.CreateTime;
                taskAdapter.SubmitTime = jobInfo.SubmitTime.HasValue ? jobInfo.SubmitTime.Value : default(DateTime);
                taskCollection.Add(ConvertTaskToTaskInfo(taskAdapter));
            }
            return taskCollection;
        }

        public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
        {
            var localHpcJobInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                    jobSpecification.ConvertToLocalHPCInfo(LinuxLocalTaskState.Q.ToString(), LinuxLocalTaskState.Q.ToString()))
                );
            StringBuilder commands = new StringBuilder();
            StringBuilder taskCommandLine = new StringBuilder();
            foreach (var task in jobSpecification.Tasks)
            {
                var commandParameterDictionary = CreateTemplateParameterValuesDictionary(
                    jobSpecification,
                    task,
                    task.CommandTemplate.TemplateParameters,
                    task.CommandParameterValues
                    );
                taskCommandLine.Append(
                    ReplaceTemplateDirectivesInCommand($"{task.CommandTemplate.ExecutableFile} {task.CommandTemplate.CommandParameters}", commandParameterDictionary));

                if (!string.IsNullOrEmpty(task.StandardOutputFile))
                {
                    taskCommandLine.Append($" 1>>{task.StandardOutputFile}");
                }
                if (!string.IsNullOrEmpty(task.StandardErrorFile))
                {
                    taskCommandLine.Append($" 2>>{task.StandardErrorFile}");
                }
                commands.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(taskCommandLine.ToString())) + " ");
                taskCommandLine.Clear();
            }

            //preparation script, prepares job info file to the job directory at local linux "cluster"
            return $"{CommandScriptPath.PrepareJobDirCmdPath} " +
                $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/ {localHpcJobInfo} \"{commands}\";";
        }

        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            List<string> jobIds = new();
            LinuxLocalInfo jobsAdapter = JsonSerializer.Deserialize<LinuxLocalInfo>(responseMessage.ToString());
            jobsAdapter.Jobs.ForEach(job => jobIds.Add(job.SchedulerJobId));
            return jobIds;
        }

        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object response)
        {
            List<SubmittedTaskInfo> taskInfos = new();
            LinuxLocalInfo jobsAdapter = JsonSerializer.Deserialize<LinuxLocalInfo>(response.ToString());
            var allTasks = jobsAdapter.Jobs;
            taskInfos.AddRange(ConvertTasksToTaskInfoCollection(jobsAdapter, allTasks));

            return taskInfos;
        }

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage)
        {
            LinuxLocalJobDTO jobAdapter = JsonSerializer.Deserialize<LinuxLocalJobDTO>(responseMessage.ToString());
            return ConvertTaskToTaskInfo(jobAdapter);
        }

        public SubmittedTaskInfo ConvertTaskToTaskInfo(LinuxLocalJobDTO jobDTO)
        {
            SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
            taskInfo.ScheduledJobId = jobDTO.SchedulerJobId.ToString();
            taskInfo.Name = jobDTO.Name;
            taskInfo.State = jobDTO.TaskState;
            taskInfo.StartTime = (jobDTO.StartTime == default(DateTime)) ? null : jobDTO.StartTime;
            taskInfo.EndTime = (jobDTO.EndTime == default(DateTime)) ? null : jobDTO.EndTime;
            taskInfo.AllocatedTime = jobDTO.AllocatedTime.TotalSeconds;
            taskInfo.TaskAllocationNodes = taskInfo.TaskAllocationNodes.Select(s => new SubmittedTaskAllocationNodeInfo
            {
                AllocationNodeId = s.ToString(),
                SubmittedTaskInfoId = long.Parse(taskInfo.Name)
            }).ToList();
            taskInfo.AllParameters = StringUtils.ConvertDictionaryToString(jobDTO.AllParametres);
            return taskInfo;
        }
        #endregion
    }
}
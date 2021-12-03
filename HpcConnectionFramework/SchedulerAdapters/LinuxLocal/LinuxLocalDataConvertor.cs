using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10;
using HEAppE.MiddlewareUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HEAppE.HpcConnectionFramework.LinuxPbs.v12;
//using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.ConversionAdapter;
using System.Text.Json;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
{
    public class LinuxLocalDataConvertor : LinuxPbsV12DataConvertor
    {
        #region Constructors
        public LinuxLocalDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) { }
        public LinuxLocalDataConvertor() : base(null) { }
        #endregion
        #region SchedulerDataConvertor Members
        protected override List<object> CreateTasks(JobSpecification jobSpecification, ISchedulerJobAdapter jobAdapter)
        {
            List<object> tasks = new List<object>();

            if (jobSpecification.Tasks != null && jobSpecification.Tasks.Count > 0)
            {

                foreach (var task in jobSpecification.Tasks)
                {

                    StringBuilder builder = new StringBuilder("");
                    builder.Append((string)ConvertTaskSpecificationToTask(jobSpecification, task, jobAdapter.Source));

                    tasks.Add(builder.ToString());
                }
            }
            return tasks;
        }

        protected override string CreateCommandLineForTask(CommandTemplate template, TaskSpecification taskSpecification,
            JobSpecification jobSpecification, Dictionary<string, string> additionalParameters)
        {
            return CreateCommandLineForTemplate(template, additionalParameters);
        }

        public override SubmittedJobInfo ConvertJobToJobInfo(object job)//TODO
        {
            SubmittedJobInfo jobInfo = new SubmittedJobInfo();
            var jobAdapter = JsonSerializer.Deserialize<LinuxLocalJobDTO>(job.ToString());
            var allTasks = jobAdapter.Tasks;
            jobInfo.Tasks = ConvertTasksToTaskInfoCollection(allTasks, jobAdapter.Id);
            jobInfo.Name = jobAdapter.Name;
            jobInfo.Project = jobAdapter.Project;
            jobInfo.State = jobAdapter.State;
            jobInfo.CreationTime = jobAdapter.CreateTime;
            jobInfo.SubmitTime = jobAdapter.SubmitTime;
            jobInfo.StartTime = jobAdapter.StartTime;
            jobInfo.EndTime = jobAdapter.EndTime;
            jobInfo.TotalAllocatedTime = CountTotalAllocatedTime(jobInfo.Tasks);
            return jobInfo;
        }

        private List<SubmittedTaskInfo> ConvertTasksToTaskInfoCollection(List<LinuxLocalTaskDTO> allTasks, long scheduledJobId)
        {
            List<SubmittedTaskInfo> taskCollection = new();
            foreach (var taskAdapter in allTasks)
            {
                SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
/*                taskInfo.TaskAllocationNodes = taskAdapter.AllocatedCoreIds?.Select(s => new SubmittedTaskAllocationNodeInfo
                {
                    AllocationNodeId = s,
                    SubmittedTaskInfoId = long.Parse(taskAdapter.Name)
                }).ToList();*/
                taskInfo.ScheduledJobId = scheduledJobId.ToString() + "." + taskAdapter.Id;
                taskInfo.Priority = taskAdapter.Priority;
                taskInfo.Name = taskAdapter.Name;
                taskInfo.State = taskAdapter.State;
                taskInfo.StartTime = taskAdapter.StartTime;
                taskInfo.EndTime = taskAdapter.EndTime;
                taskInfo.ErrorMessage = taskAdapter.ErrorMessage;
                taskInfo.AllocatedTime = taskAdapter.AllocatedTime;
                taskInfo.AllParameters = StringUtils.ConvertDictionaryToString(taskAdapter.AllParametres);
                taskCollection.Add(taskInfo);
            }
            return taskCollection;
        }

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object task)
        {
            SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
            ISchedulerTaskAdapter taskAdapter = conversionAdapterFactory.CreateTaskAdapter(task);
            taskInfo.TaskAllocationNodes = taskAdapter.AllocatedCoreIds?.Select(s => new SubmittedTaskAllocationNodeInfo
            {
                AllocationNodeId = s,
                SubmittedTaskInfoId = long.Parse(taskAdapter.Name)
            }).ToList();
            taskInfo.ScheduledJobId = taskAdapter.Id;
            taskInfo.Priority = taskAdapter.Priority;
            taskInfo.Name = taskAdapter.Name;
            taskInfo.State = taskAdapter.State;
            taskInfo.StartTime = taskAdapter.StartTime;
            taskInfo.EndTime = taskAdapter.EndTime;
            taskInfo.ErrorMessage = taskAdapter.ErrorMessage;
            taskInfo.AllocatedTime = taskAdapter.AllocatedTime;
            taskInfo.AllParameters = StringUtils.ConvertDictionaryToString(taskAdapter.AllParameters);
            return taskInfo;
        }

        public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object job)
        {
            var localHpcJobInfo =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(jobSpecification.ConvertToLocalHPCInfo("Q", "Q")));
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
                taskCommandLine.Append(CreateCommandLineForTemplate(task.CommandTemplate, commandParameterDictionary));

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
            return $"~/.key_scripts/prepare_job_dir.sh " +
                $"{jobSpecification.FileTransferMethod.Cluster.LocalBasepath}/{jobSpecification.Id}/ {localHpcJobInfo} \"{commands}\";";
        }
        #endregion
    }
}
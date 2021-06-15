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

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal {
	public class LinuxLocalDataConvertor : LinuxPbsV12DataConvertor {
		#region Constructors
		public LinuxLocalDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) {}
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
                    string varName = "_" + task.Id;
                    builder.Append(varName);
                    builder.Append("=$(");
                    builder.Append((string)ConvertTaskSpecificationToTask(jobSpecification, task, jobAdapter.Source));
                    builder.Append(");echo $");
                    builder.Append(varName);
                    builder.Append(";");

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
            ISchedulerJobAdapter jobAdapter = conversionAdapterFactory.CreateJobAdapter(job);
            List<object> allTasks = jobAdapter.GetTaskList();
            jobInfo.Tasks = ConvertAllTasksToTaskInfos(allTasks);
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

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object task)
        {
            SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
            ISchedulerTaskAdapter taskAdapter = conversionAdapterFactory.CreateTaskAdapter(task);
            taskInfo.TaskAllocationNodes = taskAdapter.AllocatedCoreIds?.Select(s=> new SubmittedTaskAllocationNodeInfo 
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
            ISchedulerJobAdapter jobAdapter = conversionAdapterFactory.CreateJobAdapter(job);
            //jobAdapter.SetRequestedResourceNumber(Convert.ToInt32(jobSpecification.MinCores),
            //    Convert.ToInt32(jobSpecification.MaxCores));
            //jobAdapter.Name = ConvertJobName(jobSpecification);
            //jobAdapter.RequestedNodeGroups = StringUtils.SplitStringToArray(jobSpecification.NodeType.RequestedNodeGroups, ',');
            jobAdapter.SetNotifications(jobSpecification.NotificationEmail,
                jobSpecification.NotifyOnStart, jobSpecification.NotifyOnFinish,
                jobSpecification.NotifyOnAbort);
            //jobAdapter.Priority = jobSpecification.Priority.Value;
            jobAdapter.Project = jobSpecification.Project;
            //jobAdapter.Queue = jobSpecification.NodeType.Queue;
            jobAdapter.AccountingString = jobSpecification.SubmitterGroup.AccountingString;
            //if (Convert.ToInt32(jobSpecification.WalltimeLimit) > 0)
            //{
            //    jobAdapter.Runtime = Convert.ToInt32(jobSpecification.WalltimeLimit);
            //}
            jobAdapter.SetTasks(CreateTasks(jobSpecification, jobAdapter));
            log.Debug(jobAdapter.Source);
            return jobAdapter.Source;
        }
        #endregion
    }
}
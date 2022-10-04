using System;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;

namespace HEAppE.ExtModels.JobReporting.Converts
{
    public static class JobReportingConverts
    {
        #region Public Methods
        public static UserResourceUsageReportExt ConvertIntToExt(this UserResourceUsageReport report)
        {
            var convert = new UserResourceUsageReportExt()
            {
                User = report.User.ConvertIntToExt(),
                NodeTypeReports = report.NodeTypeReport.Select(s => s.ConvertIntToExt())
                                                        .ToArray(),
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                TotalCorehoursUsage = report.TotalUsage
            };

            return convert;
        }

        public static UserGroupResourceUsageReportExt ConvertIntToExt(this UserGroupResourceUsageReport report)
        {
            var convert = new UserGroupResourceUsageReportExt()
            {
                UserReports = report.UserReport.Select(s => s.ConvertIntToExt())
                                                .ToArray(),
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                TotalCorehoursUsage = report.TotalUsage
            };

            return convert;
        }


        public static JobStateAggregationReportExt ConvertIntToExt(this JobStateAggregationReport aggregateJob)
        {
            var state = aggregateJob.State.ConvertIntToExt();
            var jobstateAggregationExt = new JobStateAggregationReportExt
            {
                JobStateId = state,
                JobStateName = state.ToString(),
                Count = aggregateJob.Count
            };

            return jobstateAggregationExt;
        }

        public static ProjectExt ConvertIntToExt(this Project project)
        {
            var taskInfoUsageReportExt = new ProjectExt
            {
                AccountingString = project.AccountingString,
                StartDate = project.StartDate,
                EndDate = project.EndDate
            };

            return taskInfoUsageReportExt;
        }

        public static SubmittedJobInfoReportExt ConvertIntToExt(this SubmittedJobInfoUsageReport jobInfo)
        {
            var jobInfoReportBriefExt = new SubmittedJobInfoReportExt
            {
                Id = jobInfo.Id,
                Name = jobInfo.Name,
                State = jobInfo.State.ConvertIntToExt(),
                Project = jobInfo.Project.ConvertIntToExt(),
                SubmitTime = jobInfo.SubmitTime,
                StartTime = jobInfo.StartTime,
                EndTime = jobInfo.EndTime,
                Submitter = jobInfo.Submitter.Username,
                SubmittedTasks = jobInfo.TasksUsageReport.Select(s => s.ConvertIntToExt())
            };

            return jobInfoReportBriefExt;
        }

        public static SubmittedJobInfoUsageReportExt ConvertUsageIntToExt(this SubmittedJobInfoUsageReport jobInfo)
        {
            var jobInfoUsageReportExt = new SubmittedJobInfoUsageReportExt
            {
                Id = jobInfo.Id,
                Name = jobInfo.Name,
                State = jobInfo.State.ConvertIntToExt(),
                Project = jobInfo.Project.ConvertIntToExt(),
                CreationTime = jobInfo.CreationTime,
                SubmitTime = jobInfo.SubmitTime,
                StartTime = jobInfo.StartTime,
                EndTime = jobInfo.EndTime,
                TotalAllocatedTime = jobInfo.TotalAllocatedTime,
                TotalCorehoursUsage = jobInfo.TotalUsage,
                SubmittedTasks = jobInfo.TasksUsageReport.Select(s => s.ConvertUsageIntToExt())
            };

            return jobInfoUsageReportExt;
        }

        public static SubmittedTaskInfoReportExt ConvertIntToExt(this SubmittedTaskInfoUsageReport taskInfo)
        {
            var taskInfoReportExt = new SubmittedTaskInfoReportExt
            {
                Id = taskInfo.Id,
                ScheduledJobId = taskInfo.ScheduledJobId,
                Name = taskInfo.Name,
                State = taskInfo.State.ConvertIntToExt(),
                StartTime = taskInfo.StartTime,
                EndTime = taskInfo.EndTime,
                CommandTemplateId = taskInfo.Specification.CommandTemplateId,
                CommandTemplateName = taskInfo.Specification.CommandTemplate.Name,
                ClusterName = taskInfo.Specification.ClusterNodeType.Cluster.Name,
                QueueName = taskInfo.Specification.ClusterNodeType.Name
            };

            return taskInfoReportExt;
        }

        public static SubmittedTaskInfoUsageReportExt ConvertUsageIntToExt(this SubmittedTaskInfoUsageReport taskInfo)
        {
            var taskInfoUsageReportExt = new SubmittedTaskInfoUsageReportExt
            {
                Id = taskInfo.Id,
                ScheduledJobId = taskInfo.ScheduledJobId,
                Name = taskInfo.Name,
                State = taskInfo.State.ConvertIntToExt(),
                StartTime = taskInfo.StartTime,
                EndTime = taskInfo.EndTime,
                CommandTemplateId = taskInfo.Specification.CommandTemplateId,
                AllocatedTime = taskInfo.AllocatedTime,
                CorehoursUsage = taskInfo.Usage
            };

            return taskInfoUsageReportExt;
        }

        public static SubmittedTaskInfoUsageReportExtendedExt ConvertIntToExt(this SubmittedTaskInfoUsageReportExtended taskInfo)
        {
            var taskInfoExtendedUsageReportExt = new SubmittedTaskInfoUsageReportExtendedExt
            {
                Id = taskInfo.Id,
                ScheduledJobId = taskInfo.ScheduledJobId,
                Name = taskInfo.Name,
                JobId = taskInfo.JobId,
                JobName = taskInfo.JobName,
                Project = taskInfo.Project.ConvertIntToExt(),
                State = taskInfo.State.ConvertIntToExt(),
                StartTime = taskInfo.StartTime,
                EndTime = taskInfo.EndTime,
                CommandTemplateId = taskInfo.Specification.CommandTemplateId,
                AllocatedTime = taskInfo.AllocatedTime,
                CorehoursUsage = taskInfo.Usage
            };

            return taskInfoExtendedUsageReportExt;
        }
        #endregion
        #region Private Methods
        private static UserAggregatedUsageExt ConvertIntToExt(this UserAggregatedUsage report)
        {
            var convert = new UserAggregatedUsageExt()
            {
                User = report.User.ConvertIntToExt(),
                NodeTypeReports = report.NodeTypeReport.Select(s => s.ConvertIntToExt())
                                                        .ToArray(),
                TotalCorehoursUsage = report.TotalUsage
            };

            return convert;
        }

        private static NodeTypeAggregatedUsageExt ConvertIntToExt(this NodeTypeAggregatedUsage report)
        {
            var convert = new NodeTypeAggregatedUsageExt()
            {
                ClusterNodeType = report.NodeType.ConvertIntToExt(),
                SubmittedTasks = report.SubmittedTasks.Select(s => s.ConvertIntToExt())
                                                       .ToArray(),
                TotalCorehoursUsage = report.TotalUsage
            };

            return convert;
        }
        #endregion
    }
}

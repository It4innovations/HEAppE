using System;
using System.Linq;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;

namespace HEAppE.ExtModels.JobReporting.Converts
{
    public static class JobReportingConverts
    {
        public static UserResourceUsageReportExt ConvertIntToExt(this UserResourceUsageReport report)
        {
            UserResourceUsageReportExt convert = new UserResourceUsageReportExt()
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

        private static NodeTypeAggregatedUsageExt ConvertIntToExt(this NodeTypeAggregatedUsage report)
        {
            NodeTypeAggregatedUsageExt convert = new NodeTypeAggregatedUsageExt()
            {
                ClusterNodeType = report.NodeType.ConvertIntToExt(),
                SubmittedTasks = report.SubmittedTasks.Select(s => s.ConvertIntToExt())
                                                      .ToArray(),
                TotalCorehoursUsage = report.TotalUsage
            };
            return convert;
        }

        public static UserGroupResourceUsageReportExt ConvertIntToExt(this UserGroupResourceUsageReport report)
        {
            UserGroupResourceUsageReportExt convert = new UserGroupResourceUsageReportExt()
            {
                UserReports = report.UserReport.Select(s => s.ConvertIntToExt())
                                                .ToArray(),
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                TotalCorehoursUsage = report.TotalUsage
            };
            return convert;
        }

        private static UserAggregatedUsageExt ConvertIntToExt(this UserAggregatedUsage report)
        {
            UserAggregatedUsageExt convert = new UserAggregatedUsageExt()
            {
                User = report.User.ConvertIntToExt(),
                NodeTypeReports = report.NodeTypeReport.Select(s => s.ConvertIntToExt())
                                                        .ToArray(),
                TotalCorehoursUsage = report.TotalUsage
            };
            return convert;
        }

        public static SubmittedJobInfoUsageReportExt ConvertIntToExt(this SubmittedJobInfoUsageReport jobInfo)
        {
            var jobInfoUsageReportExt = new SubmittedJobInfoUsageReportExt
            {
                Id = jobInfo.Id,
                Name = jobInfo.Name,
                State = jobInfo.State.ConvertIntToExt(),
                Project = jobInfo.Project,
                CreationTime = jobInfo.CreationTime,
                SubmitTime = jobInfo.SubmitTime,
                StartTime = jobInfo.StartTime,
                EndTime = jobInfo.EndTime,
                TotalAllocatedTime = jobInfo.TotalAllocatedTime,
                TotalCorehoursUsage = jobInfo.TotalUsage,
                SubmittedTasks = jobInfo.TasksUsageReport.Select(s => s.ConvertIntToExt())
                                                        .ToList()
            };
            return jobInfoUsageReportExt;
        }

        public static SubmittedTaskInfoUsageReportExt ConvertIntToExt(this SubmittedTaskInfoUsageReport taskInfo)
        {
            var taskInfoUsageReportExt = new SubmittedTaskInfoUsageReportExt
            {
                Id = taskInfo.Id,
                Name = taskInfo.Name,
                State = taskInfo.State.ConvertIntToExt(),
                CommandTemplateId = taskInfo.CommandTemplateId,
                ScheduledJobId = taskInfo.ScheduledJobId,
                AllocatedTime = taskInfo.AllocatedTime,
                CorehoursUsage = taskInfo.Usage,
                StartTime = taskInfo.StartTime,
                EndTime = taskInfo.EndTime
            };
            return taskInfoUsageReportExt;
        }

        public static SubmittedTaskInfoExtendedUsageReportExt ConvertIntToExt(this SubmittedTaskInfoExtendedUsageReport taskInfo)
        {
            var taskInfoExtendedUsageReportExt = new SubmittedTaskInfoExtendedUsageReportExt
            {
                Id = taskInfo.Id,
                Name = taskInfo.Name,
                JobId = taskInfo.JobId,
                JobName = taskInfo.JobName,
                Project = taskInfo.Project,
                State = taskInfo.State.ConvertIntToExt(),
                CommandTemplateId = taskInfo.CommandTemplateId,
                ScheduledJobId = taskInfo.ScheduledJobId,
                AllocatedTime = taskInfo.AllocatedTime,
                CorehoursUsage = taskInfo.Usage,
                StartTime = taskInfo.StartTime,
                EndTime = taskInfo.EndTime
            };
            return taskInfoExtendedUsageReportExt;
        }
    }
}

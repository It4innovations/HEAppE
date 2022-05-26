using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting;
using System;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting.Converts
{
    /// <summary>
    /// Job reporting convertor
    /// </summary>
    internal static class JobReportingLogicConverts
    {

        /// <summary>
        /// Convert submitted job to object for usage reporting
        /// </summary>
        /// <param name="job">Job</param>
        /// <returns></returns>
        internal static SubmittedJobInfoUsageReport ConvertToUsageReport(this SubmittedJobInfo job)
        {
            var jobInfoUsageReport = new SubmittedJobInfoUsageReport
            {
                Id = job.Id,
                Name = job.Name,
                State = job.State,
                Project = job.Project,
                CreationTime = job.CreationTime,
                SubmitTime = job.SubmitTime,
                StartTime = job.StartTime,
                EndTime = job.EndTime,
                TotalAllocatedTime = job.TotalAllocatedTime ?? 0,
                TasksUsageReport = job.Tasks.Select(s => s.ConvertToUsageReport())
            };
            jobInfoUsageReport.TotalUsage = jobInfoUsageReport.TasksUsageReport.Sum(s => s.Usage);
            return jobInfoUsageReport;
        }

        /// <summary>
        /// Convert submitted task to object for usage reporting
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns></returns>
        internal static SubmittedTaskInfoUsageReport ConvertToUsageReport(this SubmittedTaskInfo task)
        {
            var taskInfoUsageReport = new SubmittedTaskInfoUsageReport
            {
                Id = task.Id,
                ScheduledJobId = task.ScheduledJobId,
                Name = task.Name,
                State = task.State,
                Specification = task.Specification,
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                AllocatedTime = task.AllocatedTime,
                Usage = CalculateUsedResourcesForTask(task)
            };

            return taskInfoUsageReport;
        }

        /// <summary>
        /// Convert submitted task to object for extended usage reporting
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="job">Job</param>
        /// <returns></returns>
        internal static SubmittedTaskInfoUsageReportExtended ConvertToExtendedUsageReport(this SubmittedTaskInfo task, SubmittedJobInfo job)
        {
            var taskInfoExtendedUsageReport = new SubmittedTaskInfoUsageReportExtended
            {
                Id = task.Id,
                ScheduledJobId = task.ScheduledJobId,
                Name = task.Name,
                JobId = job.Id,
                JobName = job.Name,
                Project = job.Project,
                State = task.State,
                Specification = task.Specification,
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                AllocatedTime = task.AllocatedTime,
                Usage = CalculateUsedResourcesForTask(task)
            };

            return taskInfoExtendedUsageReport;
        }

        /// <summary>
        /// Convert usage report to object for aggregate usage report
        /// </summary>
        /// <param name="usageReport">User usage report</param>
        /// <returns></returns>
        internal static UserAggregatedUsage ConvertUsageReportToAggregatedUsage(this UserResourceUsageReport usageReport)
        {
            var userAggregatedUsage = new UserAggregatedUsage
            {
                User = usageReport.User,
                NodeTypeReport = usageReport.NodeTypeReport,
                TotalUsage = usageReport.TotalUsage
            };

            return userAggregatedUsage;
        }
        #region Private methods
        /// <summary>
        /// Calculate used resources for task
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns></returns>
        private static double? CalculateUsedResourcesForTask(SubmittedTaskInfo task)
        {
            if (task.State >= TaskState.Running)
            {
                double walltimeInSeconds = task.AllocatedTime ?? 0;
                int ncpus = task.AllocatedCores ?? task.Specification.MaxCores ?? 0;

                return Math.Round((walltimeInSeconds * ncpus) / 3600, 3);
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}

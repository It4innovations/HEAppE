using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                TotalAllocatedTime = job.TotalAllocatedTime ?? 0
            };

            jobInfoUsageReport.TasksUsageReport = job.Tasks.Select(s => s.ConvertToUsageReport());
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
                Name = task.Name,
                Priority = task.Priority,
                State = task.State,
                CpuHyperThreading = task.CpuHyperThreading ?? false,
                ScheduledJobId = task.ScheduledJobId,
                CommandTemplateId = task.Specification.CommandTemplate.Id,
                AllocatedTime = task.AllocatedTime,
                StartTime = task.StartTime,
                Usage = CalculateUsedResourcesForTask(task),
                EndTime = task.EndTime
            };

            return taskInfoUsageReport;
        }

        /// <summary>
        /// Convert submitted task to object for extended usage reporting
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="job">Job</param>
        /// <returns></returns>
        internal static SubmittedTaskInfoExtendedUsageReport ConvertToExtendedUsageReport(this SubmittedTaskInfo task, SubmittedJobInfo job)
        {
            var taskInfoExtendedUsageReport = new SubmittedTaskInfoExtendedUsageReport
            {
                Id = task.Id,
                Name = task.Name,
                JobId = job.Id,
                JobName = job.Name,
                Priority = task.Priority,
                State = task.State,
                CpuHyperThreading = task.CpuHyperThreading ?? false,
                ScheduledJobId = task.ScheduledJobId,
                CommandTemplateId = task.Specification.CommandTemplate.Id,
                AllocatedTime = task.AllocatedTime,
                StartTime = task.StartTime,
                Usage = CalculateUsedResourcesForTask(task),
                EndTime = task.EndTime
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
                double agregationCoreHours = 0;
                var splittedSubTasks = task.AllParameters.Split("<JOB_ARRAY_ITERATION>\r\n");
                var subTasks = string.IsNullOrEmpty(task.Specification.JobArrays)
                                        ? splittedSubTasks
                                        : splittedSubTasks.Skip(1);

                foreach (var subTask in subTasks)
                {
                    TimeSpan walltime = task.StartTime.HasValue && task.EndTime.HasValue
                                       ? task.EndTime.Value - task.StartTime.Value
                                       : TimeSpan.FromSeconds(0);

                    int ncpus = task.Specification.MaxCores ?? 0;
                    var taskAllParamsFromScheduler = subTask.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(item => item.Split(": "))
                                                              .ToDictionary(s => s[0], s => s[1]);

                    //TODO resources_used.walltime get dynamically
                    if (taskAllParamsFromScheduler.TryGetValue("resources_used.walltime", out string parsedWalltime))
                    {
                        walltime = TimeSpan.Parse(parsedWalltime);
                    }

                    //TODO resources_used.ncpus get dynamically
                    if (taskAllParamsFromScheduler.TryGetValue("resources_used.ncpus", out string parsedNumOfCPUs) && int.TryParse(parsedNumOfCPUs, out int numOfCPUs))
                    {
                        ncpus = numOfCPUs;
                    }

                    //Corehours for task
                    agregationCoreHours += Math.Round((walltime.TotalSeconds * ncpus) / 3600, 3);
                }
                return agregationCoreHours;
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}

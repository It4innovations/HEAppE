using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using System;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting.Converts
{
    /// <summary>
    /// Job reporting convertor
    /// </summary>
    internal static class JobReportingLogicConverts
    {
        #region Internal methods
        /// <summary>
        /// Calculate used resources for task
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns></returns>
        internal static double? CalculateUsedResourcesForTask(SubmittedTaskInfo task)
        {
            if (task.State >= TaskState.Running)
            {
                double walltimeInSeconds = task.AllocatedTime ?? 0;
                int ncpus = task.AllocatedCores ?? task.Specification.MaxCores ?? 0;
                double nNodes = Math.Ceiling((double)ncpus / task.Specification.ClusterNodeType.CoresPerNode);
                return task.Project.UsageType switch
                {
                    UsageType.NodeHours => Math.Round(walltimeInSeconds * nNodes / 3600.0, 3),
                    UsageType.CoreHours => Math.Round(walltimeInSeconds * ncpus / 3600.0, 3),
                    _ => null,
                };
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}

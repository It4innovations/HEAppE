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

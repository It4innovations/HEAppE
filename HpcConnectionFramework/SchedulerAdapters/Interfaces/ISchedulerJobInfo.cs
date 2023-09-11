using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    /// <summary>
    /// IScheduler job info
    /// </summary>
    public interface ISchedulerJobInfo
    {
        #region Properties
        /// <summary>
        /// Job scheduled id
        /// </summary>
        string SchedulerJobId { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Job priority
        /// Job priority
        /// </summary>
        long Priority { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        bool Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        string QueueName { get; set; }

        /// <summary>
        /// Job task state
        /// </summary>
        TaskState TaskState { get; }

        /// <summary>
        /// Job creation time
        /// </summary>
        DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        DateTime? StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        DateTime? EndTime { get; set; }

        /// <summary>
        /// Job allocated time (requirement)
        /// </summary>
        TimeSpan AllocatedTime { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job run number of cores
        /// </summary>
        int? UsedCores { get; set; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>

        IEnumerable<string> AllocatedNodes { get; }

        /// <summary>
        /// Job scheduler response raw data
        /// </summary>
        string SchedulerResponseParameters { get; }
        #endregion
    }
}

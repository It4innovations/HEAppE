using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.DTO
{
    public class PbsProQueueInfo
    {
        #region Properties
        /// <summary>
        /// Queue assigned nodes
        /// </summary>
        [Scheduler("resources_assigned.nodect")]
        public int NodesUsed { get; set; }

        /// <summary>
        /// Queue priority
        /// </summary>
        [Scheduler("Priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Queue total jobs
        /// </summary>
        [Scheduler("total_jobs")]
        public int TotalJobs { get; set; }
        #endregion
    }
}

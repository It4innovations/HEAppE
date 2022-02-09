using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro
{
    public class PbsProJobInfo : ISchedulerJobInfo
    {
        #region Instances
        /// <summary>
        /// Job allocated nodes
        /// </summary>
        private IEnumerable<string> _allocatedNodes;
        #endregion
        #region Properties
        /// <summary>
        /// Job scheduled id
        /// </summary>
        [Scheduler("Job Id")]
        public string SchedulerJobId { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        [Scheduler("Job_Name")]
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        [Scheduler("Priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        [Scheduler("Rerunable")]
        public int Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        [Scheduler("queue")]
        public string QueueName { get; set; }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; }

        /// <summary>
        /// Job exit status
        /// </summary>
        [Scheduler("Exit_status")]
        public int ExitStatus { get; set; }

        /// <summary>
        /// Job creation time
        /// </summary>
        [Scheduler("ctime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        [Scheduler("etime")]
        public DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        [Scheduler("stime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Job allocated time (requirement)
        /// </summary>
        public TimeSpan AllocatedTime { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        [Scheduler("Resource_List.walltime")]
        public TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        [Scheduler("exec_host")]
        public string AllocatedNodesSplit
        {
            set
            {
                //TODO
                _allocatedNodes = value.Split(" ");
            }
        }

        public IEnumerable<string> AllocatedNodes
        {
            get
            {
                return _allocatedNodes;
            }
        }

        /// <summary>
        /// Job scheduler response raw data
        /// </summary>
        public string SchedulerResponseParameters { get; private set; }
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerResponseParameters"></param>
        public PbsProJobInfo(string schedulerResponseParameters)
        {
            SchedulerResponseParameters = schedulerResponseParameters;
        }
        #endregion
    }
}

using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO
{
    /// <summary>
    /// Slurm job info
    /// </summary>
    public class SlurmJobInfo : ISchedulerJobInfo
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
        [Scheduler("JobId")]
        public string SchedulerJobId { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        [Scheduler("JobName")]
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        [Scheduler("Priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        [Scheduler("Requeue")]
        public int Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        [Scheduler("Partition")]
        public string QueueName { get; set; }

        /// <summary>
        /// Task state name
        /// </summary>
        [Scheduler("JobState")]
        public string StateName
        {
            set
            {
                TaskState = MappingTaskState(value).Map();
                AggregateTaskState = TaskState;
            }
        }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; private set; }

        /// <summary>
        /// Job creation time
        /// </summary>
        [Scheduler("SubmitTime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        [Scheduler("SubmitTime")]
        public DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        [Scheduler("StartTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        [Scheduler("EndTime")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Job allocated time (requirement)
        /// </summary>
        [Scheduler("TimeLimit")]
        public TimeSpan AllocatedTime { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        [Scheduler("RunTime")]
        public TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        [Scheduler("NodeList")]
        public string AllocatedNodesSplit
        {
            set
            {
                _allocatedNodes = Mapper.GetAllocatedNodes(value);
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
        #region Job Arrays Properties
        /// <summary>
        /// Array job Id (only for job arrray)
        /// </summary>
        [Scheduler("ArrayJobId")]
        public string ArrayJobId { get; set; }

        /// <summary>
        /// Array task Id (only for job arrray)
        /// </summary>
        [Scheduler("ArrayTaskId")]
        public string ArrayTaskId { get; set; }

        /// <summary>
        /// Aggregate job scheduler raw response for data
        /// </summary>
        public string AggregateSchedulerResponseParameters { get; set; }

        /// <summary>
        /// Aggregate task state
        /// </summary>
        public TaskState AggregateTaskState { get; set; }
        #endregion
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerResponseParameters"></param>
        public SlurmJobInfo(string schedulerResponseParameters)
        {
            SchedulerResponseParameters = schedulerResponseParameters;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Method: Mapping task state from text representation of state
        /// </summary>
        /// <param name="state">Task state</param>
        /// <returns></returns>
        private static SlurmTaskState MappingTaskState(string state)
        {
            state = state.Replace("_", "")
                         .Trim()
                         .ToLower();
            return Enum.TryParse(state, true, out SlurmTaskState taskState)
                    ? taskState
                    : SlurmTaskState.Failed;
        }
        #endregion
    }
}

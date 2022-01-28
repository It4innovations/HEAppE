using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO
{
    /// <summary>
    /// Slurm job DTO
    /// </summary>
    public class SlurmJobDTO
    {
        #region Instances
        /// <summary>
        /// Job allocated nodes
        /// </summary>
        private string _allocatedNodes;
        #endregion
        #region Properties
        /// <summary>
        /// Job scheduled id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        public int Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; private set; }

        /// <summary>
        /// Task state name
        /// </summary>
        public string StateName
        {
            set
            {
                TaskState = MappingTaskState(value).Map();
            }
        }

        /// <summary>
        /// Job creation time
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        public DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
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
        /// Job allocated nodes
        /// </summary>
        public string AllocatedNodesSplit
        {       
            set 
            { 
                _allocatedNodes = value;
                AllocatedNodes = Mapper.GetAllocatedNodes(_allocatedNodes);
            }
        }

        public IEnumerable<string> AllocatedNodes { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        public TimeSpan RunTime { get; set; }

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
        public SlurmJobDTO(string schedulerResponseParameters)
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

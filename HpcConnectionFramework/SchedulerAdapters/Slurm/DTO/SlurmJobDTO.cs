using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.Enums;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO
{
    /// <summary>
    /// Class: Slurm job DTO
    /// </summary>
    public class SlurmJobDTO
    {
        #region Instances
        /// <summary>
        /// Job status
        /// </summary>
        private string _stateName;

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        private string _allocatedNodes;
        #endregion
        #region Properties
        /// <summary>
        /// Job scheduled id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Job work directory
        /// </summary>
        public string WorkDirectory { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        public int Requeue { get; set; }

        /// <summary>
        /// Job account name
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; set; }

        /// <summary>
        /// Job state
        /// </summary>
        public JobState State { get; set; }

        /// <summary>
        /// Job state name
        /// </summary>
        public string StateName
        {
            get { return _stateName; }
            set
            {
                _stateName = value;
                var slurmState = MappingJobState(value);
                State = SlurmMapper.MappingJobState(slurmState);
                TaskState = SlurmMapper.MappingTaskState(slurmState);
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
                AllocatedNodes = SlurmConversionUtils.GetAllocatedNodes(_allocatedNodes);
            }
        }

        public ICollection<string> AllocatedNodes { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        public TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job scheduler response raw data
        /// </summary>
        public Dictionary<string, string> SchedulerResponseParameters { get; set; }
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public SlurmJobDTO()
        {
            SchedulerResponseParameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerParameters"></param>
        public SlurmJobDTO(Dictionary<string, string> schedulerParameters)
        {
            SchedulerResponseParameters = schedulerParameters;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Method: Mapping job state from text representation of state
        /// </summary>
        /// <param name="jobState">Job state</param>
        /// <returns></returns>
        private SlurmJobState MappingJobState(string jobState)
        {
            jobState = jobState.Replace("_", "").Replace(" ", "").ToLower();
            return Enum.TryParse(jobState, true, out SlurmJobState state)
                ? state
                : SlurmJobState.Failed;
        }
        #endregion
    }
}

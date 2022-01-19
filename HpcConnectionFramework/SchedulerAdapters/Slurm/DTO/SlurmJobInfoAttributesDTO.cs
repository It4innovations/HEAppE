using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO
{
    /// <summary>
    /// Class: Slurm Attributes (Parsing result of job from Scontrol)
    /// </summary>
    public sealed class SlurmJobInfoAttributesDTO
    {
        #region Properties
        /// <summary>
        /// Scheduled Job id
        /// </summary>
        public List<string> Id { get; }

        /// <summary>
        /// Scheduled Job name
        /// </summary>
        public List<string> Name { get; }

        /// <summary>
        /// Job priority
        /// </summary>
        public List<string> Priority { get; }

        /// <summary>
        /// Job work directory
        /// </summary>
        public List<string> WorkDirectory { get; }

        /// <summary>
        /// Job queue
        /// </summary>
        public List<string> QueueName { get; }

        /// <summary>
        /// Job project
        /// </summary>
        public List<string> AccountName { get; }

        /// <summary>
        /// Job requeue
        /// </summary>
        public List<string> Requeue { get; }

        /// <summary>
        /// Job state
        /// </summary>
        public List<string> StateName { get; }

        /// <summary>
        /// Job creation time
        /// </summary>
        public List<string> CreationTime { get; }

        /// <summary>
        /// Job submition time
        /// </summary>
        public List<string> SubmitTime { get; }

        /// <summary>
        /// Job start time
        /// </summary>
        public List<string> StartTime { get; }

        /// <summary>
        /// Job end time
        /// </summary>
        public List<string> EndTime { get; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        public List<string> AllocatedNodesSplit { get; }

        /// <summary>
        /// Job allocated time
        /// </summary>
        public List<string> AllocatedTime { get; }

        /// <summary>
        /// Job running time
        /// </summary>
        public List<string> RunTime { get; }
        #endregion
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public SlurmJobInfoAttributesDTO()
        {
            //TODO Loading from config
            Id = new List<string>() { "JobId" };
            Name = new List<string>() { "JobName" };
            Priority = new List<string>() { "Priority" };
            WorkDirectory = new List<string>() { "WorkDir" };
            AccountName = new List<string>() { "Account" };
            QueueName = new List<string>() { "Queue" };
            Requeue = new List<string>() { "Requeue" };
            StateName = new List<string>() { "JobState" };

            SubmitTime = new List<string>() { "SubmitTime" };
            CreationTime = SubmitTime;
            StartTime = new List<string>() { "StartTime" };
            EndTime = new List<string>() { "EndTime" };
            AllocatedNodesSplit = new List<string>() { "NodeList" };
            AllocatedTime = new List<string>() { "TimeLimit" };
            RunTime = new List<string>() { "RunTime" };
        }
        #endregion
    }
}

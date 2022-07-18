using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;
using HEAppE.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO
{
    /// <summary>
    /// Local Linux job information object
    /// </summary>
    public class LinuxLocalJobDTO : ISchedulerJobInfo
    {
        #region Properties
        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            set
            {
                SchedulerJobId = value.ToString();
            }
        }

        /// <summary>
        /// Job scheduled id
        /// </summary>
        public string SchedulerJobId { get; set; }

        /// <summary>
        /// Job Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        public long Priority { get; set; }


        /// <summary>
        /// Job requeue
        /// </summary>
        public bool Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Job state name
        /// </summary>
        [JsonPropertyName("State")]
        public string StateIdentifier
        {
            get { return StateIdentifier; }
            set
            {
                TaskState = MappingTaskState(value).Map();
            }
        }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; private set; }

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
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        public DateTime? EndTime { get; set; }

        [JsonPropertyName(nameof(AllocatedTime))]
        public long _allocatedTime
        {
            set
            {
                AllocatedTime = TimeSpan.FromSeconds(value);
            }
        }

        /// <summary>
        /// Job allocated time (requirement)
        /// </summary>
        [JsonIgnore]
        public TimeSpan AllocatedTime { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        public TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job run number of cores
        /// </summary>
        public int? UsedCores { get; set; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        public IEnumerable<string> AllocatedNodes { get; set; }


        /// <summary>
        /// Job scheduler response raw data
        /// </summary>
        public string SchedulerResponseParameters => StringUtils.ConvertDictionaryToString(AllParametres);

        [JsonIgnore]
        public Dictionary<string, string> AllParametres
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    {"Id", SchedulerJobId},
                    {nameof(Name), Name},
                    {nameof(Priority), Priority.ToString()},
                    {nameof(Requeue), Requeue.ToString()},
                    {nameof(TaskState), TaskState.ToString()},
                    {nameof(CreationTime), CreationTime == default(DateTime)?string.Empty:CreationTime.ToString()},
                    {nameof(SubmitTime), SubmitTime == default(DateTime)?string.Empty:SubmitTime.ToString()},
                    {nameof(StartTime), StartTime.HasValue?StartTime.ToString():string.Empty},
                    {nameof(EndTime),  EndTime.HasValue?EndTime.ToString():string.Empty},
                    {nameof(AllocatedTime), AllocatedTime.ToString()},
                    {nameof(RunTime), RunTime.ToString()},
                    {nameof(AllocatedNodes), AllocatedNodes?.ToString()},
                };
            }
        }
#endregion
        #region Methods
        /// <summary>
        /// Mapping Task state
        /// </summary>
        /// <param name="value">Value</param>
        private static LinuxLocalTaskState MappingTaskState(string value)
        {
            return Enum.TryParse(value, true, out LinuxLocalTaskState taskState)
                    ? taskState
                    : LinuxLocalTaskState.O;
        }
        #endregion
    }
}

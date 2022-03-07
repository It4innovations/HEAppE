using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;
using HEAppE.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO
{
    public class LinuxLocalJobDTO : ISchedulerJobInfo
    {
        public string SchedulerJobId { get; set; }

        public long Id
        {
            set
            {
                SchedulerJobId = value.ToString();
            }
        }
        public string Name { get; set; }
        public int Priority { get; set; }
        public bool Requeue { get; set; }
        public string QueueName { get; set; }

        [JsonPropertyName("State")]
        public string StateIdentifier
        {
            get { return StateIdentifier; }
            set
            {
                TaskState = MappingTaskState(value).Map();
            }
        }

        public TaskState TaskState { get; private set; }

        public DateTime CreationTime { get; set; }




        public DateTime SubmitTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [JsonPropertyName(nameof(AllocatedTime))]
        public long _allocatedTime
        {
            set
            {
                AllocatedTime = TimeSpan.FromSeconds(value);
            }
        }
        [JsonIgnore]
        public TimeSpan AllocatedTime { get; set; }
        public TimeSpan RunTime { get; set; }

        public int? UsedCores { get; set; }

        public IEnumerable<string> AllocatedNodes { get; set; }

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

        private static LinuxLocalTaskState MappingTaskState(string value)
        {
            return Enum.TryParse(value, true, out LinuxLocalTaskState taskState)
                    ? taskState
                    : LinuxLocalTaskState.O;
        }
    }
}

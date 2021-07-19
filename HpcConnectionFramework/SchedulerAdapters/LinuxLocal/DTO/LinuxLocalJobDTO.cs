using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO
{
    public class LinuxLocalJobDTO
    {
        public long Id { get; set; }
        public DateTime? SubmitTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("State")]
        public char InternalState { private get; set; }
        [JsonIgnore]
        public JobState State
        {
            get
            {
                switch (InternalState)
                {
                    case 'H':
                        return JobState.Configuring; 
                    case 'Q':
                        return JobState.Queued;
                    case 'O':
                        return JobState.Failed;
                    case 'R':
                        return JobState.Running;
                    case 'F':
                        return JobState.Finished;
                    case 'S':
                        return JobState.Canceled;
                    default:
                        throw new ApplicationException("Job state could not be converted to any known job state.");
                }
            }
        }

        public string Name { get; set; }
        public string Project { get; set; }
        public List<LinuxLocalTaskDTO> Tasks { get; set; }

    }
}

using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO
{
    public class LinuxLocalTaskDTO
    {
        public long Id { get; set; } = 0;
        public long JobId { get; set; } = 0;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("State")]
        public char InternalState { private get; set; }
        [JsonIgnore]
        public TaskState State
        {
            get
            {
                switch (InternalState)
                {
                    case 'H':
                        return TaskState.Configuring;
                    case 'R':
                        return TaskState.Running;
                    case 'F':
                        return TaskState.Finished;
                    case 'S':
                        return TaskState.Canceled;
                    default:
                        throw new ApplicationException("Job state could not be converted to any known job state.");
                }
            }
        }
        public string Name { get; set; }
        public long AllocatedTime { get; set; }
        public TaskPriority Priority { get; internal set; } = TaskPriority.Average;
        public string ErrorMessage { get; internal set; }
        public List<SubmittedTaskAllocationNodeInfo> AllocatedCoreIds { get; internal set; }
        public Dictionary<string, string> AllParametres { get => LoadAllParametres(); }

        private Dictionary<string, string> LoadAllParametres()
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.Add(nameof(Id), Id.ToString());
            dictionary.Add(nameof(JobId), JobId.ToString());
            dictionary.Add(nameof(StartTime), StartTime?.ToString());
            dictionary.Add(nameof(EndTime), EndTime?.ToString());
            dictionary.Add(nameof(InternalState), InternalState.ToString());
            dictionary.Add(nameof(State), State.ToString());
            dictionary.Add(nameof(Name), Name);
            dictionary.Add(nameof(AllocatedTime), AllocatedTime.ToString());
            dictionary.Add(nameof(Priority), Priority.ToString());
            dictionary.Add(nameof(ErrorMessage), ErrorMessage);
            return dictionary;
        }


        //TODO ALL PARAMETRES
    }
}

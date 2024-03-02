using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Enums;
using System.Text.Json.Serialization;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO
{
    public class HyperQueueJobInfo : ISchedulerJobInfo
    {
        private Job Job { get; set; }

        public string SchedulerJobId
        {
            get => Job.Info.Id.ToString();
            set { }
        }

        public string Name
        {
            get => Job.Info.Name;
            set { }
        }

        public long Priority
        {
            get => Job.Priority;
            set { }
        }

        public bool Requeue
        {
            get => false;
            set { }
        }

        public string QueueName
        {
            get => Job.Program.Args.FirstOrDefault();
            set { }
        }

        public TaskState TaskState
        {
            get
            {
                if (Job.Tasks == null || Job.Tasks.Count == 0)
                {
                    return TaskState.Configuring;
                }
                return Job.Info.TaskStats.GetGlobalState();
            }
        }

        public DateTime CreationTime
        {
            get => DateTime.MinValue;
            set { }
        }

        public DateTime SubmitTime
        {
            get => DateTime.MinValue;
            set { }
        }

        public DateTime? StartTime
        {
            get => Job.Tasks?.Count > 0 ? Job.Tasks.First().StartedAt : (DateTime?)null;
            set { }
        }

        public DateTime? EndTime
        {
            get => Job.Tasks?.Count > 0 ? Job.Tasks.Last().FinishedAt : (DateTime?)null;
            set { }
        }

        public TimeSpan AllocatedTime
        {
            get => TimeSpan.FromSeconds(Job.TimeLimit); // Convert the float to a TimeSpan.
            set { }
        }

        public TimeSpan RunTime
        {
            get
            {
                if (StartTime.HasValue && EndTime.HasValue)
                {
                    return EndTime.Value - StartTime.Value;
                }
                return TimeSpan.Zero;
            }
            set { }
        }

        public int? UsedCores
        {
            get => Job.Info.Resources?.Cpus.CpusValue;
            set { }
        }

        public IEnumerable<string> AllocatedNodes
        {
            get => null;
        }

        public string SchedulerResponseParameters
        {
            get => Job.ToString();
        }

        public HyperQueueJobInfo()
        {
            
        }    
        public HyperQueueJobInfo(Job job)
        {
            Job = job;
        }
    }
}

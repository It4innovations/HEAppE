using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "SubmittedTaskInfoUsageReportExt")]
    public class SubmittedTaskInfoUsageReportExt
    {
        [DataMember(Name = "Id")]
        public long Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Priority")]
        public TaskPriorityExt? Priority { get; set; }

        [DataMember(Name = "State")]
        public TaskStateExt? State { get; set; }

        [DataMember(Name = "CpuHyperThreading")]
        public bool CpuHyperThreading { get; set; }

        [DataMember(Name = "ScheduledJobId")]
        public string ScheduledJobId { get; set; }

        [DataMember(Name = "CommandTemplateId")]
        public long? CommandTemplateId { get; set; }

        [DataMember(Name = "AllocatedTime")]
        public double? AllocatedTime { get; set; }

        [DataMember(Name = "CorehoursUsage")]
        public double? CorehoursUsage { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        public override string ToString()
        {
            return $"SubmittedTaskInfoUsageReportExt(id={Id}; name={Name}; priority={Priority}; state={State}; cpuHyperThreading={CpuHyperThreading}; schedulerJobId={ScheduledJobId}; commandTemplateId={CommandTemplateId}; allocatedTime={AllocatedTime}; corehoursUsage={CorehoursUsage}; startTime={StartTime}; endTime={EndTime})";
        }
    }
}

using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.DomainObjects.JobReporting
{
    public class SubmittedTaskInfoUsageReport
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public TaskPriority Priority { get; set; }

        public TaskState State { get; set; }

        public bool? CpuHyperThreading { get; set; }

        public string ScheduledJobId { get; set; }

        public long CommandTemplateId { get; set; }

        public double? AllocatedTime { get; set; }

        public double? Usage { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}

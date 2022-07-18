using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;

namespace HEAppE.DomainObjects.JobReporting
{
    public class SubmittedTaskInfoUsageReport
    {
        public long Id { get; set; }

        public string ScheduledJobId { get; set; }

        public string Name { get; set; }

        public TaskState State { get; set; }

        public TaskSpecification Specification { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public double? AllocatedTime { get; set; }

        public double? Usage { get; set; }
    }
}

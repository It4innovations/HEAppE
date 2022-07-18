using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting
{
    public class SubmittedJobInfoUsageReport : AggregatedUsage
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public JobState State { get; set; }

        public string Project { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? SubmitTime { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public double? TotalAllocatedTime { get; set; }

        public AdaptorUser Submitter { get; set; }

        public IEnumerable<SubmittedTaskInfoUsageReport> TasksUsageReport { get; set; }
    }
}
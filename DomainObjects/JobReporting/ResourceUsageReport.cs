using System;

namespace HEAppE.DomainObjects.JobReporting
{
    public abstract class ResourceUsageReport
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public double? TotalUsage { get; set; }
    }
}
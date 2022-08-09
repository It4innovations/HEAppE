using System;

namespace HEAppE.DomainObjects.JobReporting
{
    public abstract class ResourceUsageReport : AggregatedUsage
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
    }
}
using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting
{
    public class ProjectAggregatedUsage : AggregatedUsage
    {
        public string ProjectName { get; set; }

        public IEnumerable<NodeTypeAggregatedUsage> NodeTypeReport { get; set; } = new List<NodeTypeAggregatedUsage>();
    }
}
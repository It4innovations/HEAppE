using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting
{
    public class NodeTypeAggregatedUsage : AggregatedUsage
    {
        public ClusterNodeType NodeType { get; set; }

        public IEnumerable<SubmittedTaskInfoUsageReportExtended> SubmittedTasks { get; set; } = new List<SubmittedTaskInfoUsageReportExtended>();
    }
}
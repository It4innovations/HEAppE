using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting {
	public class NodeTypeAggregatedUsage : AggregatedUsage {
		public ClusterNodeType NodeType { get; set; }
        public IEnumerable<SubmittedTaskInfoExtendedUsageReport> SubmittedTasks { get; set; } = new List<SubmittedTaskInfoExtendedUsageReport>();
	}
}
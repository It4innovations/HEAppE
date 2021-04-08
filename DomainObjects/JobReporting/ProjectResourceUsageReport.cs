using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting {
	public class ProjectResourceUsageReport : ResourceUsageReport {
        public virtual List<ProjectAggregatedUsage> ProjectReport { get; set; } = new List<ProjectAggregatedUsage>();
	}
}
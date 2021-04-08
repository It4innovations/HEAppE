using System.Collections.Generic;

namespace HEAppE.DomainObjects.JobReporting {
	public class UserGroupResourceUsageReport : ResourceUsageReport {
        public virtual IEnumerable<UserAggregatedUsage> UserReport { get; set; } = new List<UserAggregatedUsage>();
	}
}
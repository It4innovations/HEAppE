using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobReporting
{
    public class UserResourceUsageReport : ResourceUsageReport
    {
        public AdaptorUser User { get; set; }

        public virtual IEnumerable<NodeTypeAggregatedUsage> NodeTypeReport { get; set; } = new List<NodeTypeAggregatedUsage>();
    }
}
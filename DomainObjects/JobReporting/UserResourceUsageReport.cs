using System.Collections.Generic;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobReporting
{
    public class UserResourceUsageReport
    {
        public double? TotalUsage { get; set; }
        public UsageType UsageType { get; set; }
        public IEnumerable<ProjectReport> Projects { get; set; }
    }
}
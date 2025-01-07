using System.Collections.Generic;
using HEAppE.DomainObjects.JobReporting.Enums;

namespace HEAppE.DomainObjects.JobReporting;

public class UserResourceUsageReport
{
    public double? TotalUsage { get; set; }
    public UsageType UsageType { get; set; }
    public IEnumerable<ProjectReport> Projects { get; set; }
}
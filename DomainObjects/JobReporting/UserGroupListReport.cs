using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobReporting;

public class UserGroupListReport
{
    public AdaptorUserGroup AdaptorUserGroup { get; set; }
    public UsageType UsageType { get; set; }
    public ProjectReport Project { get; set; }
    public double? TotalUsage => Project.TotalUsage;
}
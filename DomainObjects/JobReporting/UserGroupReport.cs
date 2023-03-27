using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.JobReporting
{
    public class UserGroupReport
    {
        public AdaptorUserGroup AdaptorUserGroup { get; set; }
        public UsageType UsageType { get; set; }
        public ProjectReport Project { get; set; }
        public double? TotalUsage => Project.TotalUsage;
    }
}

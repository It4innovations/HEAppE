using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserAggregatedUsageExt")]
    public class UserAggregatedUsageExt
    {
        [DataMember(Name = "User")]
        public AdaptorUserExt User { get; set; }

        [DataMember(Name = "NodeTypeReports")]
        public NodeTypeAggregatedUsageExt[] NodeTypeReports { get; set; }

        [DataMember(Name = "TotalCorehoursUsage")]
        public double? TotalCorehoursUsage { get; set; }

        public override string ToString()
        {
            return $"UserAggregatedUsageExt(user={User}; nodeTypeReports={NodeTypeReports}; totalCorehoursUsage={TotalCorehoursUsage})";
        }
    }
}

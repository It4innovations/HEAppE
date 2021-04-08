using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserResourceUsageReportExt")]
    public class UserResourceUsageReportExt
    {
        [DataMember(Name = "User")]
        public AdaptorUserExt User { get; set; }

        [DataMember(Name = "NodeTypeReports")]
        public NodeTypeAggregatedUsageExt[] NodeTypeReports { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "TotalCorehoursUsage")]
        public double? TotalCorehoursUsage { get; set; }

        public override string ToString()
        {
            return $"UserResourceUsageReportExt(user={User}; nodeTypeReports={NodeTypeReports}; startTime={StartTime}; endTime={EndTime}; totalCorehoursUsage={TotalCorehoursUsage})";
        }
    }
}

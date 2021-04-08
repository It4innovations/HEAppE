using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserGroupResourceUsageReportExt")]
    public class UserGroupResourceUsageReportExt
    {
        [DataMember(Name = "UserReports")]
        public UserAggregatedUsageExt[] UserReports { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "TotalCorehoursUsage")]
        public double? TotalCorehoursUsage { get; set; }

        public override string ToString()
        {
            return $"UserGroupResourceUsageReportExt(userReports={UserReports}; startTime={StartTime}; endTime={EndTime}; totalCorehoursUsage={TotalCorehoursUsage})";
        }
    }
}

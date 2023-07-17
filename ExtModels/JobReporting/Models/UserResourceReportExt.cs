using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserResourceReportExt")]
    public class UserResourceReportExt
    {
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public UsageTypeExt UsageType { get; set; }
        [DataMember]
        public IEnumerable<ProjectReportExt> Projects { get; set; }

    }
}

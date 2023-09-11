using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport
{
    [DataContract(Name = "UserGroupDetailedReportExt")]
    public class UserGroupDetailedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public UsageTypeExt UsageType { get; set; }
        [DataMember]
        public ProjectDetailedReportExt Project { get; set; }
    }
}

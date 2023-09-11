using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserGroupReportExt")]
    public class UserGroupReportExt
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
        public ProjectReportExt Project { get; set; }
    }
}

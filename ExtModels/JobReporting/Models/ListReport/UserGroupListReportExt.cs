using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.ListReport
{
    [DataContract(Name = "UserGroupReportExt")]
    public class UserGroupListReportExt
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
        public ProjectListReportExt Project { get; set; }

    }
}

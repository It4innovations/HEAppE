using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "ProjectReportExt")]
    public class ProjectAggregatedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string AccountingString { get; set; }
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public UsageTypeExt UsageType { get; set; }
        [DataMember]
        public List<SubProjectAggregatedReportExt> SubProjects { get; set; }
        public List<ClusterAggregatedReportExt> Clusters { get; set; }
    }
}

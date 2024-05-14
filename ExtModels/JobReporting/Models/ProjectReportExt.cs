using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "ProjectReportExt")]
    public class ProjectReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string AccountingString { get; set; }
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public List<ClusterReportExt> Clusters { get; set; }
    }
}

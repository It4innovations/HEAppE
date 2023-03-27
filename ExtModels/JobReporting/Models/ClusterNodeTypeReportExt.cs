using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "ClusterNodeTypeReportExt")]
    public class ClusterNodeTypeReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public List<JobReportExt> Jobs { get; set; }
    }
}

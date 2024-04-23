using System.Collections.Generic;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "ClusterReportExt")]
    public class ClusterAggregatedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        public double? TotalUsage { get; set; }
        [DataMember]
        public List<ClusterNodeTypeAggregatedReportExt> ClusterNodeTypesAggregations { get; set; }
    }
}

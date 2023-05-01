using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport
{
    public class ClusterDetailedReportExt
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
        public List<ClusterNodeTypeDetailedReportExt> ClusterNodeTypes { get; set; }
    }
}
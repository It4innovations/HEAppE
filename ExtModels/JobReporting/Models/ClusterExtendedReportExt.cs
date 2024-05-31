using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "ClusterExtendedReportExt")]
    public class ClusterExtendedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember]
        public List<ClusterNodeExtendedTypeReportExt> ClusterNodeTypes { get; set; }
    }
}
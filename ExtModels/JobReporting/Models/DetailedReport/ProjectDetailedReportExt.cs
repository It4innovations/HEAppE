using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport
{
    [DataContract(Name = "ProjectDetailedReportExt")]
    public class ProjectDetailedReportExt
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
        public DateTime StartDate { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public List<ClusterDetailedReportExt> Clusters { get; set; }
    }
}

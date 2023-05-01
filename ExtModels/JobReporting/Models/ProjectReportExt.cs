using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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

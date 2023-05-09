using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport
{
    [DataContract(Name = "UserGroupDetailedReportExt")]
    public class UserGroupDetailedReportExt
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
        public ProjectDetailedReportExt Project { get; set; }
    }
}

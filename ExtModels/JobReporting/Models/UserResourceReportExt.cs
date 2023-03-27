using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "UserResourceReportExt")]
    public class UserResourceReportExt
    {
        [DataMember]
        public double? TotalUsage { get; set; }
        [DataMember] 
        public UsageTypeExt UsageType { get; set; }
        [DataMember]
        public IEnumerable<ProjectReportExt> Projects { get; set; }

    }
}

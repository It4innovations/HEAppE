using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "JobReportExt")]
    public class JobReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public JobStateExt State { get; set; }
        [DataMember]
        public List<TaskReportExt> Tasks { get; set; }
    }
}

using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport
{
    [DataContract(Name = "JobDetailedReportExt")]
    public class JobDetailedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public JobStateExt State { get; set; }
        [DataMember]
        public DateTime CreationTime { get; set; }
        [DataMember]
        public DateTime? SubmitTime { get; set; }
        [DataMember]
        public DateTime? StartTime { get; set; }
        [DataMember]
        public DateTime? EndTime { get; set; }
        [DataMember]
        public string Submitter { get; set; }
        [DataMember]
        public List<TaskDetailedReportExt> Tasks { get; set; }
    }
}

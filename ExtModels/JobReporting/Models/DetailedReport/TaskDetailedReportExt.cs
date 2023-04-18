using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "TaskDetailedReportExt")]
    public class TaskDetailedReportExt
    {
        [DataMember]
        public long Id { get; set; }
        [DataMember]
        public string ScheduledJobId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public TaskStateExt State { get; set; }
        [DataMember]
        public DateTime? StartTime { get; set; }
        [DataMember]
        public DateTime? EndTime { get; set; }
        [DataMember]
        public long CommandTemplateId { get; set; }
        [DataMember]
        public string CommandTemplateName { get; set; }
        [DataMember]
        public double? Usage { get; set; }
    }
}

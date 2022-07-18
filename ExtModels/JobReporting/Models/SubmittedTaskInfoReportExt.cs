using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "SubmittedTaskInfoReportExt")]
    public class SubmittedTaskInfoReportExt
    {
        #region Properties
        [DataMember(Name = "Id")]
        public long Id { get; set; }

        [DataMember(Name = "ScheduledJobId")]
        public string ScheduledJobId { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "State")]
        public TaskStateExt State { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "CommandTemplateId")]
        public long CommandTemplateId { get; set; }

        [DataMember(Name = "CommandTemplateName")]
        public string CommandTemplateName { get; set; }

        [DataMember(Name = "ClusterName")]
        public string ClusterName { get; set; }

        [DataMember(Name = "QueueName")]
        public string QueueName { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"SubmittedTaskInfoUsageReportBriefExt: Id={Id}, SchedulerJobId={ScheduledJobId}, Name={Name}, State={State}, StartTime={StartTime}, EndTime={EndTime}, CommandTemplateId={CommandTemplateId}, CommandTemplateName={CommandTemplateName}, ClusterName={ClusterName}, QueueName={QueueName}";
        }
        #endregion
    }
}

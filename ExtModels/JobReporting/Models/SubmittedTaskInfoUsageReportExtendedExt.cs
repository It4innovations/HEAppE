using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "SubmittedTaskInfoExtendedUsageReportExt")]
    public class SubmittedTaskInfoUsageReportExtendedExt: SubmittedTaskInfoUsageReportExt
    {
        [DataMember(Name = "JobId")]
        public long JobId { get; set; }

        [DataMember(Name = "JobName")]
        public string JobName { get; set; }

        [DataMember(Name = "Project")]
        public ProjectExt Project { get; set; }
        public override string ToString()
        {
            return $"SubmittedTaskInfoUsageExtendedReportExt(id={Id}; name={Name}; jobId={JobId}; jobName={JobName}; project={Project} state={State}; schedulerJobId={ScheduledJobId}; commandTemplateId={CommandTemplateId}; allocatedTime={AllocatedTime}; corehoursUsage={CorehoursUsage}; startTime={StartTime}; endTime={EndTime})";
        }
    }
}

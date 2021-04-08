using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "SubmittedTaskInfoExtendedUsageReportExt")]
    public class SubmittedTaskInfoExtendedUsageReportExt: SubmittedTaskInfoUsageReportExt
    {
        [DataMember(Name = "JobId")]
        public long JobId { get; set; }

        [DataMember(Name = "JobName")]
        public string JobName { get; set; }

        public override string ToString()
        {
            return $"SubmittedTaskInfoUsageExtendedReportExt(id={Id}; name={Name}; jobId={JobId}; jobName={JobName}; priority={Priority}; state={State}; cpuHyperThreading={CpuHyperThreading}; schedulerJobId={ScheduledJobId}; commandTemplateId={CommandTemplateId}; allocatedTime={AllocatedTime}; corehoursUsage={CorehoursUsage}; startTime={StartTime}; endTime={EndTime})";
        }
    }
}

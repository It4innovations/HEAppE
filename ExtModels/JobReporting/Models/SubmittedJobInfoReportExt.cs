using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "SubmittedJobInfoReportExt")]
    public class SubmittedJobInfoReportExt
    {
        #region Properties
        [DataMember(Name = "Id")]
        public long Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "State")]
        public JobStateExt State { get; set; }

        [DataMember(Name = "Project")]
        public string Project { get; set; }

        [DataMember(Name = "SubmitTime")]
        public DateTime? SubmitTime { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "SubmittedTasks")]
        public IEnumerable<SubmittedTaskInfoReportExt> SubmittedTasks { get; set; }

        [DataMember(Name = "Submitter")]
        public string Submitter { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"SubmittedJobInfoReportBriefExt: Id={Id}, Name={Name}, State={State}, Project={Project}, SubmitTime={SubmitTime}, StartTime={StartTime}, EndTime={EndTime}, SubmittedTasks={SubmittedTasks}, Submitter={Submitter}";
        }
        #endregion
    }
}

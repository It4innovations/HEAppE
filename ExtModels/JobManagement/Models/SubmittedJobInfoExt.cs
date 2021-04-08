using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "SubmittedJobInfoExt")]
    public class SubmittedJobInfoExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "State")]
        public JobStateExt? State { get; set; }

        [DataMember(Name = "Project")]
        public string Project { get; set; }

        [DataMember(Name = "CreationTime")]
        public DateTime? CreationTime { get; set; }

        [DataMember(Name = "SubmitTime")]
        public DateTime? SubmitTime { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "TotalAllocatedTime")]
        public double? TotalAllocatedTime { get; set; }

        [DataMember(Name = "Tasks")]
        public SubmittedTaskInfoExt[] Tasks { get; set; }

        public override string ToString()
        {
            return $"SubmittedJobInfoExt(id={Id}; name={Name}; state={State}; project={Project}; creationTime={CreationTime}; submitTime={SubmitTime}; startTime={StartTime}; endTime={EndTime}; totalAllocatedTime={TotalAllocatedTime}; tasks={Tasks})";
        }
    }
}

using HEAppE.ExtModels.ClusterInformation.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "SubmittedTaskInfoExt")]
    public class SubmittedTaskInfoExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "State")]
        public TaskStateExt? State { get; set; }

        [DataMember(Name = "Priority")]
        public TaskPriorityExt? Priority { get; set; }

        [DataMember(Name = "AllocatedTime")]
        public double? AllocatedTime { get; set; }

        [DataMember(Name = "AllocatedCoreIds")]
        public string[] AllocatedCoreIds { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime? StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime? EndTime { get; set; }

        [DataMember(Name = "NodeType")]
        public ClusterNodeTypeForTaskExt NodeType { get; set; }

        [DataMember(Name = "ErrorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "CpuHyperThreading")]
        public bool? CpuHyperThreading { get; set; }

        public override string ToString()
        {
            return $"SubmittedTaskInfoExt(id={Id}; name={Name}; state={State}; priority={Priority}; allocatedTime={AllocatedTime}; allocatedCoreIds={AllocatedCoreIds}; startTime={StartTime}; endTime={EndTime}; nodeType={NodeType}; errorMessage={ErrorMessage})";
        }
    }
}

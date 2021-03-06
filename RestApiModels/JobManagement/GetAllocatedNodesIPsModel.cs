using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "GetAllocatedNodesIPsModel")]
    public class GetAllocatedNodesIPsModel : SessionCodeModel
    {
        [DataMember(Name = "SubmittedTaskInfoId")]
        public long SubmittedTaskInfoId { get; set; }
    }
}

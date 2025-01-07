using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "GetAllocatedNodesIPsModel")]
public class AllocatedNodesIPsModel : SessionCodeModel
{
    [DataMember(Name = "SubmittedTaskInfoId")]
    public long SubmittedTaskInfoId { get; set; }

    public override string ToString()
    {
        return $"GetAllocatedNodesIPsModel({base.ToString()}; SubmittedTaskInfoId: {SubmittedTaskInfoId})";
    }
}
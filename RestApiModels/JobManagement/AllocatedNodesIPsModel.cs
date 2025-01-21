using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for retrieving list of allocated nodes IPs
/// </summary>
[DataContract(Name = "GetAllocatedNodesIPsModel")]
[Description("Model for retrieving list of allocated nodes IPs")]
public class AllocatedNodesIPsModel : SessionCodeModel
{
    /// <summary>
    /// Submitted task info id
    /// </summary>
    [DataMember(Name = "SubmittedTaskInfoId")]
    [Description("Submitted task info id")]
    public long SubmittedTaskInfoId { get; set; }

    public override string ToString()
    {
        return $"GetAllocatedNodesIPsModel({base.ToString()}; SubmittedTaskInfoId: {SubmittedTaskInfoId})";
    }
}
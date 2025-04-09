using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to cancel job
/// </summary>
[DataContract(Name = "CancelJobModel")]
[Description("Model to cancel job")]
public class CancelJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"CancelJobModel({base.ToString()})";
    }
}
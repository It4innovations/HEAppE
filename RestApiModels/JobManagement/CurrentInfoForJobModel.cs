using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for retrieving current info for job
/// </summary>
[DataContract(Name = "GetCurrentInfoForJobModel")]
[Description("Model for retrieving current info for job")]
public class CurrentInfoForJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"GetCurrentInfoForJobModel({base.ToString()})";
    }
}
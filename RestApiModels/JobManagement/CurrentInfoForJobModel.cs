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
    /// <summary>
    /// Force direct status query on cluster scheduler (overrides callback DB cache)
    /// </summary>
    [DataMember(Name = "ForceDirectQuery")]
    [Description("Force direct status query on cluster scheduler (overrides callback DB cache)")]
    public bool ForceDirectQuery { get; set; } = false;

    public override string ToString()
    {
        return $"GetCurrentInfoForJobModel({base.ToString()})";
    }
}
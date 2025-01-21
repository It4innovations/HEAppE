using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for submitting job
/// </summary>
[DataContract(Name = "SubmitJobModel")]
[Description("Model for submitting job")]
public class SubmitJobModel : CreatedJobInfoModel
{
    public override string ToString()
    {
        return $"SubmitJobModel({base.ToString()})";
    }
}
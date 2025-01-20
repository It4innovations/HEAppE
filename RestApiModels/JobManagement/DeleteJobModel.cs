using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to delete job
/// </summary>
[DataContract(Name = "DeleteJobModel")]
[Description("Model to delete job")]
public class DeleteJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"DeleteJobModel({base.ToString()})";
    }
}
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "GetCurrentInfoForJobModel")]
public class CurrentInfoForJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"GetCurrentInfoForJobModel({base.ToString()})";
    }
}
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "SubmitJobModel")]
public class SubmitJobModel : CreatedJobInfoModel
{
    public override string ToString()
    {
        return $"SubmitJobModel({base.ToString()})";
    }
}
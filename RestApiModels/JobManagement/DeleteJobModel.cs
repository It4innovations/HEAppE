using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "DeleteJobModel")]
public class DeleteJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"DeleteJobModel({base.ToString()})";
    }
}
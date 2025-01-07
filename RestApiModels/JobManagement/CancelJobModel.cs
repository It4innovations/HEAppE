using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "CancelJobModel")]
public class CancelJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"CancelJobModel({base.ToString()})";
    }
}
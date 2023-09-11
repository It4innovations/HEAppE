using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CancelJobModel")]
    public class CancelJobModel : SubmittedJobInfoModel
    {
        public override string ToString()
        {
            return $"CancelJobModel({base.ToString()})";
        }
    }
}

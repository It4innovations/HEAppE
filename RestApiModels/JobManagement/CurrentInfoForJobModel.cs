using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "GetCurrentInfoForJobModel")]
    public class CurrentInfoForJobModel : SubmittedJobInfoModel
    {
        public override string ToString()
        {
            return $"GetCurrentInfoForJobModel({base.ToString()})";
        }
    }
}

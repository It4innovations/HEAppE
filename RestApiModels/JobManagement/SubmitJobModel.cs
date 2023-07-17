using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "SubmitJobModel")]
    public class SubmitJobModel : CreatedJobInfoModel
    {
        public override string ToString()
        {
            return $"SubmitJobModel({base.ToString()})";
        }
    }
}

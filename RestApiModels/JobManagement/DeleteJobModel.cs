using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "DeleteJobModel")]
    public class DeleteJobModel : SubmittedJobInfoModel
    {
        public override string ToString()
        {
            return $"DeleteJobModel({base.ToString()})";
        }
    }
}

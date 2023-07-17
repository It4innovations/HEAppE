using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "ListJobsForCurrentUserModel")]
    public class ListJobsForCurrentUserModel : SessionCodeModel
    {
        public override string ToString()
        {
            return $"ListJobsForCurrentUserModel({base.ToString()})";
        }
    }
}

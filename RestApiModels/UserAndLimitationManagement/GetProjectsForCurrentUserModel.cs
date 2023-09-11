using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "GetProjectsForCurrentUserModel")]
    public class GetProjectsForCurrentUserModel : SessionCodeModel
    {
        public override string ToString()
        {
            return $"GetProjectsForCurrentUserModel({base.ToString()})";
        }
    }
}

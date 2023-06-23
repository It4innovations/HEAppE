using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "GetCurrentUsageAndLimitationsForCurrentUserModel")]
    public class GetCurrentUsageAndLimitationsForCurrentUserModel : SessionCodeModel
    {
        public override string ToString()
        {
            return $"GetCurrentUsageAndLimitationsForCurrentUserModel({base.ToString()})";
        }
    }
}

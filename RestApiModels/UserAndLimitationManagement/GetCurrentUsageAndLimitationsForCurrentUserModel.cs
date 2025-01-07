using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

[DataContract(Name = "GetCurrentUsageAndLimitationsForCurrentUserModel")]
public class GetCurrentUsageAndLimitationsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"GetCurrentUsageAndLimitationsForCurrentUserModel({base.ToString()})";
    }
}
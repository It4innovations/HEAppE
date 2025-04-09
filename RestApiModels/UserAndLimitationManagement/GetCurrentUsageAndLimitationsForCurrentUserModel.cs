using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model for retrieving current usage and limitations for current user
/// </summary>
[DataContract(Name = "GetCurrentUsageAndLimitationsForCurrentUserModel")]
[Description("Model for retrieving current usage and limitations for current user")]
public class GetCurrentUsageAndLimitationsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"GetCurrentUsageAndLimitationsForCurrentUserModel({base.ToString()})";
    }
}
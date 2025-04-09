using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model for retrieving projects for current user
/// </summary>
[DataContract(Name = "GetProjectsForCurrentUserModel")]
[Description("Model for retrieving projects for current user")]
public class GetProjectsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"GetProjectsForCurrentUserModel({base.ToString()})";
    }
}
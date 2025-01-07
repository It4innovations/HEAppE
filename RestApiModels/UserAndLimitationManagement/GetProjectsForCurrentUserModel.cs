using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

[DataContract(Name = "GetProjectsForCurrentUserModel")]
public class GetProjectsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"GetProjectsForCurrentUserModel({base.ToString()})";
    }
}
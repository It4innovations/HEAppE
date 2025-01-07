using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

[DataContract(Name = "ListAdaptorUserGroupsModel")]
public class ListAdaptorUserGroupsModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ListAdaptorUserGroupsModel({base.ToString()})";
    }
}
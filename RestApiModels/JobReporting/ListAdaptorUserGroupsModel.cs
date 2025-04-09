using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

/// <summary>
/// Model for retrieving list of adaptor user groups
/// </summary>
[DataContract(Name = "ListAdaptorUserGroupsModel")]
[Description("Model for retrieving list of adaptor user groups")]
public class ListAdaptorUserGroupsModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ListAdaptorUserGroupsModel({base.ToString()})";
    }
}
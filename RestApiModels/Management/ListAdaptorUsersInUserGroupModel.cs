using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List adaptor users in user group model
/// </summary>
[DataContract(Name = "ListAdaptorUsersInUserGroupModel")]
[Description("List adaptor users in user group model")]
public class ListAdaptorUsersInUserGroupModel : SessionCodeModel
{
    /// <summary>
    /// UserGroup Id
    /// </summary>
    [DataMember(Name = "UserGroupId", IsRequired = true)]
    [Description("UserGroupId")]
    public long UserGroupId { get; set; }
    
    public override string ToString()
    {
        return $"ListAdaptorUsersInUserGroupModel: UserGroupId={UserGroupId}";
    }
}
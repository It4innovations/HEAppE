using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Assign adaptor user to user group model
/// </summary>
[DataContract(Name = "AssignAdaptorUserToUserGroupModel")]
[Description("Assign adaptor user to user group model")]
public class AssignAdaptorUserToUserGroupModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }
    
    /// <summary>
    /// UserGroup Id
    /// </summary>
    [DataMember(Name = "UserGroupId", IsRequired = true)]
    [Description("UserGroupId")]
    public long UserGroupId { get; set; }
    
    /// <summary>
    /// Role in the project
    /// </summary>
    [DataMember(Name = "Role", IsRequired = true)]
    [Description("Role")]
    public AdaptorUserRoleType Role { get; set; }
    
    public override string ToString()
    {
        return $"AssignAdaptorUserToUserGroupModel: Username={Username}, UserGroupId={UserGroupId}, Role={Role}";
    }
}
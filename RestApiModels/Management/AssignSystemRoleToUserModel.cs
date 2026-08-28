using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Assign system role to user model
/// </summary>
[DataContract(Name = "AssignSystemRoleToUserModel")]
[Description("Assign system role to user model")]
public class AssignSystemRoleToUserModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Role to assign
    /// </summary>
    [DataMember(Name = "Role", IsRequired = true)]
    [Description("Role to assign")]
    public AdaptorUserRoleType Role { get; set; }

    public override string ToString()
    {
        return $"AssignSystemRoleToUserModel: Username={Username}, Role={Role}";
    }
}

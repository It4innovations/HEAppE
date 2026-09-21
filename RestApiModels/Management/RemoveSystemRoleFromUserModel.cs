using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove system role from user model
/// </summary>
[DataContract(Name = "RemoveSystemRoleFromUserModel")]
[Description("Remove system role from user model")]
public class RemoveSystemRoleFromUserModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Role to remove
    /// </summary>
    [DataMember(Name = "Role", IsRequired = true)]
    [Description("Role to remove")]
    public AdaptorUserRoleType Role { get; set; }

    public override string ToString()
    {
        return $"RemoveSystemRoleFromUserModel: Username={Username}, Role={Role}";
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement.Extensions;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

[Table("AdaptorUserRole")]
public class AdaptorUserRole : IdentifiableDbEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [Required] [StringLength(200)] public string Description { get; set; }

    public virtual List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new();

    [NotMapped] public AdaptorUserRoleType RoleType => (AdaptorUserRoleType)Id;

    [NotMapped] public IEnumerable<AdaptorUserRoleType> ContainedRoleTypes => RoleType.GetAllowedRolesForUserRoleType();

    public override string ToString()
    {
        return $"AdaptorUserRole: Id={Id}, Name={Name}";
    }
}
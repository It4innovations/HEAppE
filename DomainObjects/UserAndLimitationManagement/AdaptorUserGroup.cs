using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

[Table("AdaptorUserGroup")]
public class AdaptorUserGroup : IdentifiableDbEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [Required] [StringLength(200)] public string Description { get; set; }

    public virtual List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new();

    [ForeignKey("Project")] public long? ProjectId { get; set; }

    public virtual Project Project { get; set; }

    [NotMapped] public List<AdaptorUser> Users => AdaptorUserUserGroupRoles?.Select(g => g.AdaptorUser).ToList();

    public override string ToString()
    {
        return $"AdaptorUserGroup: Id={Id}, Name={Name}, Project={Project}";
    }
}
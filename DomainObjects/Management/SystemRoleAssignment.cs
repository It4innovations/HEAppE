using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;

namespace HEAppE.DomainObjects.Management;

[Table("SystemRoleAssignment")]
public class SystemRoleAssignment : IdentifiableDbEntity
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; }

    [Required]
    public AdaptorUserRoleType Role { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string Source { get; set; }

    public override string ToString()
    {
        return $"SystemRoleAssignment: Id={Id}, Username={Username}, Role={Role}, Source={Source}, CreatedAt={CreatedAt}";
    }
}

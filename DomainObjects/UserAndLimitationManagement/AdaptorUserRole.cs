using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserRole")]
    public class AdaptorUserRole : IdentifiableDbEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }
        
        [ForeignKey("AdaptorUserRole")]
        [Obsolete]
        public long? ParentRoleId { get; set; }
        [Obsolete]
        public virtual AdaptorUserRole ParentRole { get; set; }
        public virtual List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new List<AdaptorUserUserGroupRole>();

        [NotMapped]
        public List<AdaptorUser> Users => AdaptorUserUserGroupRoles?.Select(g => g.AdaptorUser).ToList();

        public override string ToString()
        {
            return $"AdaptorUserRole: Id={Id}, Name={Name}";
        }
    }
}
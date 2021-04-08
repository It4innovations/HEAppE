using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public virtual List<AdaptorUserUserRole> AdaptorUserUserRoles { get; set; } = new List<AdaptorUserUserRole>();

        [NotMapped]
        public List<AdaptorUser> Users => AdaptorUserUserRoles?.Select(g => g.AdaptorUser).ToList();

        public override string ToString()
        {
            return $"AdaptorUserRole: Id={Id}, Name={Name}";
        }
    }
}
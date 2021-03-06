using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserGroup")]
    public class AdaptorUserGroup : IdentifiableDbEntity {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [StringLength(50)]
        public string AccountingString { get; set; }

        public virtual List<AdaptorUserUserGroup> AdaptorUserUserGroups { get; set; } = new List<AdaptorUserUserGroup>();

        [NotMapped]
        public List<AdaptorUser> Users => AdaptorUserUserGroups?.Select(g => g.AdaptorUser).ToList();

        public override string ToString() {
            return String.Format("AdaptorUserGroup: Id={0}, Name={1}, AccountingString={2}", Id, Name, AccountingString);
        }
    }
}
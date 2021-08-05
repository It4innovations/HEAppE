using HEAppE.DomainObjects.Logging;
using HEAppE.DomainObjects.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUser")]
    public class AdaptorUser : IdentifiableDbEntity, ILogUserIdentification {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(50)]
        public string Password { get; set; }

        [Column(TypeName = "text")]
        public string PublicKey { get; set; }

        public bool Synchronize { get; set; }

        public bool Deleted { get; set; }

        //TODO Adding Created At and ModifiedAt property!!!

        public virtual Language Language { get; set; }

        public virtual List<AdaptorUserUserGroup> AdaptorUserUserGroups { get; set; } = new List<AdaptorUserUserGroup>();

        public virtual List<AdaptorUserUserRole> AdaptorUserUserRoles { get; set; } = new List<AdaptorUserUserRole>();

        public virtual List<ResourceLimitation> Limitations { get; set; } = new List<ResourceLimitation>();

        [NotMapped]
        public List<AdaptorUserGroup> Groups => AdaptorUserUserGroups?.Select(g => g.AdaptorUserGroup).ToList();

        [NotMapped]
        public List<AdaptorUserRole> Roles => AdaptorUserUserRoles?.Select(g => g.AdaptorUserRole).ToList();

        /// <summary>
        /// Check if user have specified user role.
        /// </summary>
        /// <param name="role">User role.</param>
        /// <returns>True if user has the specified role.</returns>
        public bool HasUserRole(AdaptorUserRole role)
        {
            if (AdaptorUserUserRoles is null)
            {
                return false;
            }
            return AdaptorUserUserRoles.Any(userRole => userRole.AdaptorUserRoleId == role.Id);
        }

        public string GetLogIdentification() {
            return Username;
        }

        public override string ToString() {
            return string.Format("AdaptorUser: Id={0}, Username={1}", Id, Username);
        }
    }
}
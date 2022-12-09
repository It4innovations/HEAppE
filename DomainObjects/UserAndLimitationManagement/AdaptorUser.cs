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
    public class AdaptorUser : IdentifiableDbEntity, ILogUserIdentification
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(128)]
        public string Password { get; set; }

        [Column(TypeName = "text")]
        public string PublicKey { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        public bool Synchronize { get; set; }

        public bool Deleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [ForeignKey("Language")]
        public long? LanguageId { get; set; }
        public virtual Language Language { get; set; }

        public virtual List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new List<AdaptorUserUserGroupRole>();

        public virtual List<ResourceLimitation> Limitations { get; set; } = new List<ResourceLimitation>();

        [NotMapped]
        public List<AdaptorUserGroup> Groups => AdaptorUserUserGroupRoles?.Select(g => g.AdaptorUserGroup).ToList();

        [NotMapped]
        public List<AdaptorUserRole> Roles => AdaptorUserUserGroupRoles?.Select(g => g.AdaptorUserRole).ToList();

        public List<AdaptorUserRole> GetRolesForProject(long projectId)
        {
            return AdaptorUserUserGroupRoles.Where(x => x.AdaptorUserGroup.ProjectId == projectId).Select(x => x.AdaptorUserRole).ToList();
        }

        /// <summary>
        /// Check if user have specified user role.
        /// </summary>
        /// <param name="role">User role.</param>
        /// <returns>True if user has the specified role.</returns>
        public bool HasUserRole(AdaptorUserRole role)
        {
            if (AdaptorUserUserGroupRoles is null)
            {
                return false;
            }
            return AdaptorUserUserGroupRoles.Any(userRole => userRole.AdaptorUserRoleId == role.Id);
        }

        public string GetLogIdentification()
        {
            return Username;
        }

        public override string ToString()
        {
            return string.Format("AdaptorUser: Id={0}, Username={1}", Id, Username);
        }
    }
}
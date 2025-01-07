using HEAppE.DomainObjects.Logging;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUser")]
    public class AdaptorUser : IdentifiableDbEntity, ILogUserIdentification, ISoftDeletableEntity
    {
        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [StringLength(128)]
        public string Password { get; set; }

        [Column(TypeName = "text")]
        public string PublicKey { get; set; }

        [StringLength(100)]
        public string Email { get; set; }
        public bool Synchronize { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [Required]
        public AdaptorUserType UserType { get; set; } = AdaptorUserType.Default;

        public virtual List<AdaptorUserUserGroupRole> AdaptorUserUserGroupRoles { get; set; } = new List<AdaptorUserUserGroupRole>();

        [NotMapped]
        public List<AdaptorUserGroup> Groups => AdaptorUserUserGroupRoles?.Select(g => g.AdaptorUserGroup).ToList();

        public string GetLogIdentification()
        {
            return Username;
        }

        /// <summary>
        /// Create Specific User Role for User
        /// </summary>
        /// <param name="group">User Group</param>
        /// <param name="roleType">Role</param>
        /// <returns></returns>
        public void CreateSpecificUserRoleForUser(AdaptorUserGroup group, AdaptorUserRoleType roleType)
        {
            AdaptorUserUserGroupRole adaptorUserWithGroupRole = AdaptorUserUserGroupRoles.FirstOrDefault(f => f.AdaptorUserGroup == group);
            if (adaptorUserWithGroupRole is null)
            {
                var adaptorUserUserGroupRole = new AdaptorUserUserGroupRole()
                {
                    AdaptorUserId = Id,
                    AdaptorUserGroup = group,
                    AdaptorUserGroupId = group.Id,
                    AdaptorUserRoleId = (long)roleType,
                    IsDeleted = false
                };

                AdaptorUserUserGroupRoles.Add(adaptorUserUserGroupRole);
            }
            else
            {
                var adaptorUserRoleType = (AdaptorUserRoleType)adaptorUserWithGroupRole.AdaptorUserRoleId;
                if (adaptorUserRoleType != roleType)
                {
                    var role = adaptorUserWithGroupRole.IsDeleted 
                        ? (long)roleType 
                        : (long)adaptorUserRoleType.GetHighestRole(roleType);

                    AdaptorUserUserGroupRoles.Remove(adaptorUserWithGroupRole);
                    var adaptorUserUserGroupRole = new AdaptorUserUserGroupRole()
                    {
                        AdaptorUserId = Id,
                        AdaptorUserGroup = group,
                        AdaptorUserGroupId = group.Id,
                        AdaptorUserRoleId = role,
                    };

                    AdaptorUserUserGroupRoles.Add(adaptorUserUserGroupRole);
                }

                adaptorUserWithGroupRole.IsDeleted = false;
            }
        }

        public override string ToString()
        {
            return string.Format("AdaptorUser: Id={0}, Username={1}", Id, Username);
        }
    }
}
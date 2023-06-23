using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserUserGroupRole")]
    public class AdaptorUserUserGroupRole
    {
        public long AdaptorUserId { get; set; }
        public virtual AdaptorUser AdaptorUser { get; set; }

        public long AdaptorUserGroupId { get; set; }
        public virtual AdaptorUserGroup AdaptorUserGroup { get; set; }

        public long AdaptorUserRoleId { get; set; }
        public virtual AdaptorUserRole AdaptorUserRole { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            return $"AdaptorUserUserGroupRole: AdaptorUser={AdaptorUser}, AdaptorUserGroup={AdaptorUserGroup}, AdaptorUserRole={AdaptorUserRole}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}";
        }
    }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserUserGroupRole")]
    public class AdaptorUserUserGroupRole {
        public long AdaptorUserId { get; set; }
        public virtual AdaptorUser AdaptorUser { get; set; }

        public long AdaptorUserGroupId { get; set; }
        public virtual AdaptorUserGroup AdaptorUserGroup { get; set; }

        public long AdaptorUserRoleId { get; set; }
        public virtual AdaptorUserRole AdaptorUserRole { get; set; }
    }
}
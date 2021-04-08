using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserUserGroup")]
    public class AdaptorUserUserGroup {
        public long AdaptorUserId { get; set; }
        public virtual AdaptorUser AdaptorUser { get; set; }

        public long AdaptorUserGroupId { get; set; }
        public virtual AdaptorUserGroup AdaptorUserGroup { get; set; }
    }
}
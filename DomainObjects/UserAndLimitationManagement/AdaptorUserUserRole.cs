using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement
{
    [Table("AdaptorUserUserRole")]
    public class AdaptorUserUserRole
    {
        public long AdaptorUserId { get; set; }
        public virtual AdaptorUser AdaptorUser { get; set; }

        public long AdaptorUserRoleId { get; set; }
        public virtual AdaptorUserRole AdaptorUserRole { get; set; }
    }
}
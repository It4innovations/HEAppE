using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackAuthenticationCredentials")]
    public class OpenStackAuthenticationCredentials : IdentifiableDbEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        [Required]
        [ForeignKey("OpenStackInstance")]
        public long OpenStackInstanceId { get; set; }

        public virtual OpenStackInstance OpenStackInstance { get; set; }

        public override string ToString()
        {
            return $"OpenStackAuthenticationCredentials: Username={Username}, OpenStackInstanceId={OpenStackInstanceId}";
        }
    }
}
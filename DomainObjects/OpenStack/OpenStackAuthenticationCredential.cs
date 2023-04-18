using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackAuthenticationCredential")]
    public class OpenStackAuthenticationCredential : IdentifiableDbEntity
    {
        [Required]
        [StringLength(80)]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        public virtual List<OpenStackAuthenticationCredentialDomain> OpenStackAuthenticationCredentialDomains { get; set; }

        public virtual List<OpenStackAuthenticationCredentialProject> OpenStackAuthenticationCredentialProjects { get; set; }

        public override string ToString()
        {
            return $"OpenStackAuthenticationCredentials: UserId={UserId}, Username={Username}";
        }
    }
}
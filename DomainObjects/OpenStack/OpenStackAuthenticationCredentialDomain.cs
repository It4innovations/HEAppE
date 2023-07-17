using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackAuthenticationCredentialDomain")]
    public class OpenStackAuthenticationCredentialDomain
    {
        [ForeignKey("OpenStackAuthenticationCredential")]
        public long OpenStackAuthenticationCredentialId { get; set; }
        public virtual OpenStackAuthenticationCredential OpenStackAuthenticationCredential { get; set; }

        [ForeignKey("OpenStackDomain")]
        public long OpenStackDomainId { get; set; }
        public virtual OpenStackDomain OpenStackDomain { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackAuthenticationCredentialProjectDomain")]
    public class OpenStackAuthenticationCredentialProjectDomain
    {

        [ForeignKey("OpenStackAuthenticationCredential")]
        public long OpenStackAuthenticationCredentialId { get; set; }
        public virtual OpenStackAuthenticationCredential OpenStackAuthenticationCredential { get; set; }

        [ForeignKey("OpenStackProjectDomain")]
        public long OpenStackProjectDomainId { get; set; }
        public virtual OpenStackProjectDomain OpenStackProjectDomain { get; set; }
    }
}

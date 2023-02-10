using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackAuthenticationCredentialProject")]
    public class OpenStackAuthenticationCredentialProject
    {
        [ForeignKey("OpenStackAuthenticationCredential")]
        public long OpenStackAuthenticationCredentialId { get; set; }
        public virtual OpenStackAuthenticationCredential OpenStackAuthenticationCredential { get; set; }

        [ForeignKey("OpenStackProject")]
        public long OpenStackProjectId { get; set; }
        public virtual OpenStackProject OpenStackProject { get; set; }

        public bool IsDefault = false;
    }
}
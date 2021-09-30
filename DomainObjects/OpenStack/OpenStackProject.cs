using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackProject")]
    public class OpenStackProject : IdentifiableDbEntity
    {
        [StringLength(40)]
        public string Name { get; set; }

        [StringLength(80)]
        public string UID { get; set; }

        [Required]
        [ForeignKey("OpenStackDomain")]
        public long OpenStackDomainId { get; set; }

        public virtual OpenStackDomain OpenStackDomain { get; set; }

        public virtual List<OpenStackProjectDomain> OpenStackProjectDomains { get; set; }
        public override string ToString()
        {
            return $"OpenStackProject: Id={Id}, Name={Name}, UID={UID}";
        }
    }
}

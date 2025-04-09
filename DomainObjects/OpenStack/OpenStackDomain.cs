using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack;

[Table("OpenStackDomain")]
public class OpenStackDomain : IdentifiableDbEntity
{
    [StringLength(40)] public string Name { get; set; }

    [StringLength(80)] public string UID { get; set; }

    [ForeignKey("OpenStackInstance")] public long OpenStackInstanceId { get; set; }

    public virtual OpenStackInstance OpenStackInstance { get; set; }

    public virtual List<OpenStackProject> OpenStackProjects { get; set; }

    public virtual List<OpenStackAuthenticationCredentialDomain> OpenStackAuthenticationCredentialDomains { get; set; }

    public override string ToString()
    {
        return $"OpenStackDomain: Id={Id}, Name={Name}, UID={UID}";
    }
}
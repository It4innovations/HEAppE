﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack;

[Table("OpenStackInstance")]
public class OpenStackInstance : IdentifiableDbEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [Required] [StringLength(70)] public string InstanceUrl { get; set; }

    public virtual List<OpenStackDomain> OpenStackDomains { get; set; }

    public override string ToString()
    {
        return $"OpenStackInstance: Id={Id}, Name={Name}, InstanceUrl={InstanceUrl}, Domains={OpenStackDomains}";
    }
}
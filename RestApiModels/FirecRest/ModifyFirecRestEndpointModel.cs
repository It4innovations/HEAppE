using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FirecRest;

[DataContract(Name = "ModifyFirecRestEndpointModel")]
[Description("Modify accounting model")]
public class ModifyFirecRestEndpointModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(200)]
    [Description("Description")]
    public string Description { get; set; }

    [DataMember(Name = "Url", IsRequired = true)]
    [StringLength(250)]
    [Description("Url")]
    public string Url { get; set; }

    [DataMember(Name = "IdpUrl", IsRequired = true)]
    [StringLength(250)]
    [Description("IdpUrl")]
    public string IdpUrl { get; set; }
}
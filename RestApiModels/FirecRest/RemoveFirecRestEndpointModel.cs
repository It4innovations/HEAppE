using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FirecRest;

[DataContract(Name = "RemoveFirecRestEndpointModel")]
[Description("Remove accounting model")]
public class RemoveFirecRestEndpointModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }
}
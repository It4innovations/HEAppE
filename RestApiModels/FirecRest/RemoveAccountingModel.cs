using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FirecRest;

/// <summary>
/// Remove accounting model
/// </summary>
[DataContract(Name = "RemoveFirecRestEndpointModel")]
[Description("Remove accounting model")]
public class RemoveFirecRestEndpointModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }
}
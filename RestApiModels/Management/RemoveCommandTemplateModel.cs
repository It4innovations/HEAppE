using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove command template mode
/// </summary>
[DataContract(Name = "RemoveCommandTemplateModel")]
[Description("Remove command template model")]
public class RemoveCommandTemplateModel : SessionCodeModel
{
    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Command template id")]
    public long CommandTemplateId { get; set; }

    public override string ToString()
    {
        return $"RemoveCommandTemplateModel({base.ToString()}; CommandTemplateId: {CommandTemplateId})";
    }
}
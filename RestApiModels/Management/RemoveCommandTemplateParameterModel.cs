using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove command template parameter model
/// </summary>
[DataContract(Name = "RemoveCommandTemplateParameterModel")]
[Description("Remove command template parameter model")]
public class RemoveCommandTemplateParameterModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"RemoveCommandTemplateParameterModel: Id={Id}";
    }
}
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List command template model
/// </summary>
[DataContract(Name = "ListCommandTemplateModel")]
[Description("List command template model")]
public class ListCommandTemplateModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"ListCommandTemplateModel({base.ToString()}; Id: {Id})";
    }
}
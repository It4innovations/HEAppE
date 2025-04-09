using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Command template parameter ext
/// </summary>
[DataContract(Name = "CommandTemplateParameterExt")]
[Description("Command template parameter ext")]
public class CommandTemplateParameterExt
{
    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier")]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    public override string ToString()
    {
        return $"CommandTemplateParameterExt(identifier={Identifier}; description={Description})";
    }
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Command template parameter value ext
/// </summary>
[DataContract(Name = "CommandTemplateParameterValueExt")]
[Description("Command template parameter value ext")]
public class CommandTemplateParameterValueExt
{
    /// <summary>
    /// Command parameter identifier
    /// </summary>
    [DataMember(Name = "CommandParameterIdentifier")]
    [StringLength(20)]
    [Description("Command parameter identifier")]
    public string CommandParameterIdentifier { get; set; }

    /// <summary>
    /// Parameter value
    /// </summary>
    [DataMember(Name = "ParameterValue")]
    [StringLength(100000)]
    [Description("Parameter value")]
    public string ParameterValue { get; set; }

    public override string ToString()
    {
        return
            $"CommandTemplateParameterValueExt(commandParameterIdentifier={CommandParameterIdentifier}; parameterValue={ParameterValue})";
    }
}
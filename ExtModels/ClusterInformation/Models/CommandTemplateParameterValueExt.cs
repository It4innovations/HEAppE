using System.ComponentModel;
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
    [Description("Command parameter identifier")]
    public string CommandParameterIdentifier { get; set; }

    /// <summary>
    /// Parameter value
    /// </summary>
    [DataMember(Name = "ParameterValue")]
    [Description("Parameter value")]
    public string ParameterValue { get; set; }

    public override string ToString()
    {
        return
            $"CommandTemplateParameterValueExt(commandParameterIdentifier={CommandParameterIdentifier}; parameterValue={ParameterValue})";
    }
}
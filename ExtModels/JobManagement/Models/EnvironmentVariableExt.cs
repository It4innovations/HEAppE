using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Environment variable model
/// </summary>
[DataContract(Name = "EnvironmentVariableExt")]
[Description("Environment variable model")]
public class EnvironmentVariableExt
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    [DataMember(Name = "Value")]
    [StringLength(100)]
    [Description("Value")]
    public string Value { get; set; }

    public override string ToString()
    {
        return $"EnvironmentVariableExt(name={Name}; value={Value})";
    }
}
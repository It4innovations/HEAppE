using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.DataTransfer.Models;

/// <summary>
///     HTTP header variable ext
/// </summary>
[DataContract(Name = "HTTPHeaderVariableExt")]
public class HTTPHeaderExt
{
    #region Override Methods

    /// <summary>
    ///     ToString
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"HTTPHeaderVariableExt(name={Name}; value={Value})";
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Name
    /// </summary>
    [DataMember(Name = "Name")]
    [StringLength(50)]
    public string Name { get; set; }

    /// <summary>
    ///     Value
    /// </summary>
    [DataMember(Name = "Value")]
    [StringLength(100)]
    public string Value { get; set; }

    #endregion
}
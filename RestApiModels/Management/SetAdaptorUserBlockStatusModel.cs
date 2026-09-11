using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Set adaptor user block status model
/// </summary>
[DataContract(Name = "SetAdaptorUserBlockStatusModel")]
[Description("Set adaptor user block status model")]
public class SetAdaptorUserBlockStatusModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Is blocked flag
    /// </summary>
    [DataMember(Name = "IsBlocked", IsRequired = true)]
    [Description("IsBlocked")]
    public bool IsBlocked { get; set; }

    public override string ToString()
    {
        return $"SetAdaptorUserBlockStatusModel: Username={Username}, IsBlocked={IsBlocked}";
    }
}

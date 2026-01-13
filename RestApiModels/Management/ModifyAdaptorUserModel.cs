using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify adaptor user model
/// </summary>
[DataContract(Name = "ModifyAdaptorUserModel")]
[Description("Modify adaptor user model")]
public class ModifyAdaptorUserModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "OldUsername", IsRequired = true)]
    [StringLength(100)]
    [Description("OldUsername")]
    public string OldUsername { get; set; }
    
    /// <summary>
    /// New Username
    /// </summary>
    [DataMember(Name = "NewUsername", IsRequired = true)]
    [StringLength(100)]
    [Description("NewUsername")]
    public string NewUsername { get; set; }
    
    public override string ToString()
    {
        return $"ModifyAdaptorUserModel: OldUsername={OldUsername}, NewUsername={NewUsername}";
    }
}
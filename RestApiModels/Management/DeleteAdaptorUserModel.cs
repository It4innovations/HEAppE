using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Delete adaptor user model
/// </summary>
[DataContract(Name = "DeleteAdaptorUserModel")]
[Description("Delete adaptor user model")]
public class DeleteAdaptorUserModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }
    
    public override string ToString()
    {
        return $"ModifyAdaptorUserModel: Username={Username}";
    }
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create adaptor user model
/// </summary>
[DataContract(Name = "CreateAdaptorUserModel")]
[Description("Create adaptor user model")]
public class CreateAdaptorUserModel : SessionCodeModel
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
        return $"CreateAdaptorUserModel: Username={Username}";
    }
}
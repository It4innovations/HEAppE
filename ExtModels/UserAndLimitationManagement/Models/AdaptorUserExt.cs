using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Adaptor user ext
/// </summary>
[DataContract(Name = "AdaptorUserExt")]
[Description("Adaptor user ext")]
public class AdaptorUserExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username")]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Public key
    /// </summary>
    [DataMember(Name = "PublicKey")]
    [Description("Public key")]
    public string PublicKey { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [DataMember(Name = "Email")]
    [Description("Email")]
    public string Email { get; set; }

    /// <summary>
    /// User type
    /// </summary>
    [DataMember(Name = "UserType")]
    [Description("User type")]
    public AdaptorUserTypeExt UserType { get; set; }

    /// <summary>
    /// Array of adaptor user groups
    /// </summary>
    [DataMember(Name = "AdaptorUserGroups")]
    [Description("Array of adaptor user groups")]
    public AdaptorUserGroupExt[] AdaptorUserGroups { get; set; }

    public override string ToString()
    {
        return $"AdaptorUserExt(id={Id}; username={Username}; publicKey={PublicKey}; email={Email}; userType={UserType}; adaptorUserGroups={AdaptorUserGroups})";
    }
}
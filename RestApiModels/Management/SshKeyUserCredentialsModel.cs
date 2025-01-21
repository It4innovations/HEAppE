using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// SSH key user credentials model
/// </summary>
[Description("SSH key user credentials model")]
public class SshKeyUserCredentialsModel
{
    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(50)]
    [Description("User name")]
    public string Username { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password", IsRequired = false)]
    [StringLength(50)]
    [Description("Password")]
    public string Password { get; set; }
}
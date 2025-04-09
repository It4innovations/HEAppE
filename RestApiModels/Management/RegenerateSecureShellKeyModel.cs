using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Regenerate secure shell key model
/// </summary>
[DataContract(Name = "RegenerateSecureShellKeyModel")]
[Description("Regenerate secure shell key model")]
public class RegenerateSecureShellKeyModel : SessionCodeModel
{
    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "Username", IsRequired = false)]
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

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
}
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove secure shell key model
/// </summary>
[DataContract(Name = "RemoveSecureShellKeyModel")]
[Description("Remove secure shell key model")]
public class RemoveSecureShellKeyModel : SessionCodeModel
{
    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [Description("User name")]
    public string Username { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
}
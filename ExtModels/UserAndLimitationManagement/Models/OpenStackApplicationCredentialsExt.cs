using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// OpenStack application credentials ext
/// </summary>
[DataContract(Name = "OpenStackApplicationCredentialsExt")]
[Description("OpenStack application credentials ext")]
public class OpenStackApplicationCredentialsExt
{
    /// <summary>
    /// Application credentials id
    /// </summary>
    [DataMember(Name = "ApplicationCredentialsId")]
    [Description("Application credentials id")]
    public string ApplicationCredentialsId { get; set; }

    /// <summary>
    /// Application credentials secret
    /// </summary>
    [DataMember(Name = "ApplicationCredentialsSecret")]
    [Description("Application credentials secret")]
    public string ApplicationCredentialsSecret { get; set; }
}
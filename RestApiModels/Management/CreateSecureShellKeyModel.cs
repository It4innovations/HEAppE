using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create secure shell key model
/// </summary>
[DataContract(Name = "CreateSecureShellKeyModel")]
[Description("Create secure shell key model")]
public class CreateSecureShellKeyModel : SessionCodeModel
{
    /// <summary>
    /// List of ssh key user credentials
    /// </summary>
    [DataMember(Name = "Credentials", IsRequired = true)]
    [Description("List of ssh key user credentials")]
    public List<SshKeyUserCredentialsModel> Credentials { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
}
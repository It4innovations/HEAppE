using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify Cluster Authentication Credential Model
/// </summary>
[DataContract(Name = "ModifyClusterAuthenticationCredentialModel")]
[Description("Create secure shell key model")]
public class ModifyClusterAuthenticationCredentialModel : SessionCodeModel
{
    /// <summary>
    /// Project ID
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project ID")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Old username
    /// </summary>
    [DataMember(Name = "OldUsername", IsRequired = true)]
    [Description("Old username")]
    public string OldUsername { get; set; }

    /// <summary>
    /// New username
    /// </summary>
    [DataMember(Name = "NewUsername", IsRequired = true)]
    [Description("New username")]
    public string NewUsername { get; set; }
    
    /// <summary>
    /// New password
    /// </summary>
    [DataMember(Name = "NewPassword", IsRequired = false)]
    [Description("New password")]
    public string NewPassword { get; set; }
}
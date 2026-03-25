#nullable enable
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create credential model
/// </summary>
[DataContract(Name = "CreateCredentialModel")]
[Description("Create credential model")]
public class CreateCredentialModel
{
    // --- Common Properties ---

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Session code
    /// </summary>
    [DataMember(Name = "SessionCode", IsRequired = false)]
    [Description("Session code")]
    public string SessionCode { get; set; }

    /// <summary>
    /// Adaptor user id (optional, for project managers acting on behalf of other users)
    /// </summary>
    [DataMember(Name = "AdaptorUserId", IsRequired = false)]
    [Description("Adaptor user id")]
    public long? AdaptorUserId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// AuthType
    /// </summary>
    [DataMember(Name = "AuthType", IsRequired = true)]
    [Description("AuthType")]
    public ClusterAuthenticationCredentialsAuthType AuthType { get; set; } 

    // --- SSH Key Specific Properties ---

    /// <summary>
    /// Generate new key
    /// </summary>
    [DataMember(Name = "GenerateNewKey", IsRequired = false)]
    [Description("Generate new key")]
    public bool? GenerateNewKey { get; set; }

    /// <summary>
    /// Provided private key
    /// </summary>
    [DataMember(Name = "ProvidedPrivateKey", IsRequired = false)]
    [Description("Provided private key")]
    public string? ProvidedPrivateKey { get; set; }

    /// <summary>
    /// Passphrase
    /// </summary>
    [DataMember(Name = "Passphrase", IsRequired = false)]
    [Description("Passphrase")]
    public string? Passphrase { get; set; }

    // --- Password Specific Properties ---

    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password", IsRequired = false)]
    [Description("Password")]
    public string? Password { get; set; }
}

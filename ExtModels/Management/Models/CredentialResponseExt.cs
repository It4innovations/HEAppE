using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;


namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Credential response model
/// </summary>
[DataContract(Name = "CredentialResponseExt")]
[Description("Credential response ext")]
public class CredentialResponseExt
{
    // --- Common Properties ---

    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username")]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// AuthType
    /// </summary>
    [DataMember(Name = "AuthType")]
    [Description("AuthType")]
    public ClusterAuthenticationCredentialsAuthType AuthType { get; set; }

    /// <summary>
    /// Is generated
    /// </summary>
    [DataMember(Name = "IsGenerated")]
    [Description("Is generated")]
    public bool IsGenerated { get; set; }

    // --- SSH Key Specific Properties ---

    /// <summary>
    /// Public key fingerprint
    /// </summary>
    [DataMember(Name = "PublicKeyFingerprint")]
    [Description("Public key fingerprint")]
    public string? PublicKeyFingerprint { get; set; }

    /// <summary>
    /// Public key ext
    /// </summary>
    [DataMember(Name = "PublicKeyExt")]
    [Description("Public key ext")]
    public string? PublicKeyExt { get; set; }

    /// <summary>
    /// Adaptor user id
    /// </summary>
    [DataMember(Name = "AdaptorUserId")]
    [Description("Adaptor user id")]
    public long? AdaptorUserId { get; set; }
}

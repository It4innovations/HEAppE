using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File tansfer key credentials ext
/// </summary>
[DataContract(Name = "FileTransferKeyCredentialsExt")]
[Description("File tansfer key credentials ext")]
public class FileTransferKeyCredentialsExt : AuthenticationCredentialsExt
{
    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password")]
    [Description("Password")]
    public string Password { get; set; }

    /// <summary>
    /// Cipher type
    /// </summary>
    [DataMember(Name = "CipherType")]
    [Description("Cipher type")]
    public FileTransferCipherTypeExt? CipherType { get; set; }

    /// <summary>
    /// Authentication credentials type
    /// </summary>
    [DataMember(Name = "CredentialsAuthType")]
    [Description("Authentication credentials type")]
    public ClusterAuthenticationCredentialsAuthTypeExt CredentialsAuthType { get; set; }

    /// <summary>
    /// Private key
    /// </summary>
    [DataMember(Name = "PrivateKey")]
    [Description("Private key")]
    public string PrivateKey { get; set; }

    /// <summary>
    /// Private key certificate
    /// </summary>
    [DataMember(Name = "PrivateKeyCertificate")]
    [Description("Private key certificate")]
    public string PrivateKeyCertificate { get; set; }

    /// <summary>
    /// Public key
    /// </summary>
    [DataMember(Name = "PublicKey")]
    [Description("Public key")]
    public string PublicKey { get; set; }

    /// <summary>
    /// Passphrase
    /// </summary>
    [DataMember(Name = "Passphrase")]
    [Description("Passphrase")]
    public string Passphrase { get; set; }

    public override string ToString()
    {
        return $"""AsymmetricKeyCredentialsExt(Username="{Username}";CipherType="{CipherType}"; CredentialsAuthType="{CredentialsAuthType}"; PublicKey="{PublicKey}; Passphrase={Passphrase}")""";
    }
}
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.ExtModels.FileTransfer.Models;

[DataContract(Name = "FileTransferKeyCredentialsExt")]
public class FileTransferKeyCredentialsExt : AuthenticationCredentialsExt
{
    [DataMember(Name = "Password")] public string Password { get; set; }

    [DataMember(Name = "CipherType")] public FileTransferCipherTypeExt? CipherType { get; set; }

    [DataMember(Name = "CredentialsAuthType")]
    public ClusterAuthenticationCredentialsAuthTypeExt CredentialsAuthType { get; set; }

    [DataMember(Name = "PrivateKey")] public string PrivateKey { get; set; }

    [DataMember(Name = "PrivateKeyCertificate")]
    public string PrivateKeyCertificate { get; set; }

    [DataMember(Name = "PublicKey")] public string PublicKey { get; set; }

    [DataMember(Name = "Passphrase")] public string Passphrase { get; set; }

    public override string ToString()
    {
        return
            $"""AsymmetricKeyCredentialsExt(Username="{Username}";CipherType="{CipherType}"; CredentialsAuthType="{CredentialsAuthType}"; PublicKey="{PublicKey}; Passphrase={Passphrase}")""";
    }
}
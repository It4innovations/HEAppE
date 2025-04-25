using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;

public class FileTransferKeyCredentials : AuthenticationCredentials
{
    public FileTransferCipherType FileTransferCipherType { get; set; }
    public ClusterAuthenticationCredentialsAuthType CredentialsAuthType { get; set; }
    public string Password { get; set; }
    public string PrivateKey { get; set; }
    public string PrivateKeyCertificate { get; set; }
    public string PublicKey { get; set; }
    public string Passphrase { get; set; }
}
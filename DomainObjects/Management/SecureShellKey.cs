using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DomainObjects.Management;

public class SecureShellKey
{
    public string Username { get; set; }
    public string Passphrase { get; set; }
    public FileTransferCipherType CipherType { get; set; }
    public string PrivateKeyPEM { get; set; }
    public string PublicKeyPEM { get; set; }
    public string PublicKeyInAuthorizedKeysFormat { get; set; }
    public string PublicKeyFingerprint { get; set; }
}
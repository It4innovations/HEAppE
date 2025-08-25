using System.ComponentModel;

namespace HEAppE.DomainObjects.ClusterInformation;

/// <summary>
/// Cluster authentication credentials types
/// </summary>
[Description("Cluster authentication credentials types")]
public enum ClusterAuthenticationCredentialsAuthTypeExt
{
    Unknown = 0,
    Password = 1,
    PasswordInteractive = 2,
    PasswordAndPrivateKey = 3,
    PrivateKey = 4,
    PasswordViaProxy = 5,
    PasswordInteractiveViaProxy = 6,
    PasswordAndPrivateKeyViaProxy = 7,
    PrivateKeyViaProxy = 8,
    PrivateKeyInSshAgent = 9,
    SshCertificate = 10,
    SshCertificateViaProxy = 11
}
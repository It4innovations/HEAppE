using System;
using System.ComponentModel;

namespace HEAppE.DomainObjects.ClusterInformation;

/// <summary>
/// Cluster authentication credentials types
/// </summary>
[Description("Cluster authentication credentials types")]
[Flags]
public enum ClusterAuthenticationCredentialsAuthTypeExt
{
    Unknown = 0,
    PasswordInteractive = 1,
    Password = 2,
    PrivateKey = 4,
    PrivateKeyInSshAgent = 8,
    PrivateKeyInVaultAndInSshAgent = 16,
    SshCertificate = 32,
    Kerberos = 64,
    FirecRestIdpViaExpirio = 128,
    PasswordAndPrivateKey = Password | PrivateKey
}
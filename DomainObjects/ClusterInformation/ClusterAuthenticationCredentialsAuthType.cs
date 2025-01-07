namespace HEAppE.DomainObjects.ClusterInformation;

public enum ClusterAuthenticationCredentialsAuthType
{
    Password = 1,
    PasswordInteractive = 2,
    PasswordAndPrivateKey = 3,
    PrivateKey = 4,
    PasswordViaProxy = 5,
    PasswordInteractiveViaProxy = 6,
    PasswordAndPrivateKeyViaProxy = 7,
    PrivateKeyViaProxy = 8,
    PrivateKeyInSshAgent = 9,
    PrivateKeyInVaultAndInSshAgent = 10
}
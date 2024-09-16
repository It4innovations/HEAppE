using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.Utils
{
    /// <summary>
    /// ClusterAuthenticationCredentialsUtils utils
    /// </summary>
    public static class ClusterAuthenticationCredentialsUtils
    {
        public static ClusterAuthenticationCredentialsAuthType GetCredentialsAuthenticationType(ClusterAuthenticationCredentials credential, Cluster cluster)
        {
            if (cluster.ProxyConnection is null)
            {
                if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKey))
                {
                    return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey;
                }

                if (!string.IsNullOrEmpty(credential.PrivateKey))
                {
                    return ClusterAuthenticationCredentialsAuthType.PrivateKey;
                }

                if (!string.IsNullOrEmpty(credential.Password))
                {
                    switch (cluster.ConnectionProtocol)
                    {
                        case ClusterConnectionProtocol.MicrosoftHpcApi:
                        return ClusterAuthenticationCredentialsAuthType.Password;

                        case ClusterConnectionProtocol.Ssh:
                        return ClusterAuthenticationCredentialsAuthType.Password;

                        case ClusterConnectionProtocol.SshInteractive:
                        return ClusterAuthenticationCredentialsAuthType.PasswordInteractive;

                        default:
                        return ClusterAuthenticationCredentialsAuthType.Password;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKey))
                {
                    return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy;
                }

                if (!string.IsNullOrEmpty(credential.PrivateKey))
                {
                    return ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy;
                }

                if (!string.IsNullOrEmpty(credential.Password))
                {
                    switch (cluster.ConnectionProtocol)
                    {
                        case ClusterConnectionProtocol.MicrosoftHpcApi:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;

                        case ClusterConnectionProtocol.Ssh:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;

                        case ClusterConnectionProtocol.SshInteractive:
                        return ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy;

                        default:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;
                    }
                }
            }

            return credential.AuthenticationType == ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent ? ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent : ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent;
        }
    }
}

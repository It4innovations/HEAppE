using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using System.Collections.Generic;
using System.Linq;
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
            if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKeyFile))
            {
              return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey;
            }

            if (!string.IsNullOrEmpty(credential.PrivateKeyFile))
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
            if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKeyFile))
            {
              return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy;
            }

            if (!string.IsNullOrEmpty(credential.PrivateKeyFile))
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

          return ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent;
        }
    }
}

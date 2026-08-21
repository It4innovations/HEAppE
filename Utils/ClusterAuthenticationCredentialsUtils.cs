using HEAppE.DomainObjects.ClusterInformation;
using SshCaAPI.Configuration;

namespace HEAppE.Utils;

/// <summary>
///     ClusterAuthenticationCredentialsUtils utils
/// </summary>
public static class ClusterAuthenticationCredentialsUtils
{
    public static ClusterAuthenticationCredentialsAuthType GetCredentialsAuthenticationType(
        ClusterAuthenticationCredentials credential, Cluster cluster)
    {
        if (credential.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Unknown)
        {
            if (cluster != null && (cluster.SchedulerType & SchedulerType.FirecRestSlurm) == SchedulerType.FirecRestSlurm)
            {
                return ClusterAuthenticationCredentialsAuthType.FirecRestIdpViaExpirio;
            }
        }

        if (cluster.ProxyConnection is null)
        {
            //skip if type is >= 13 - other than classic 
            if (credential.AuthenticationType >= ClusterAuthenticationCredentialsAuthType.Kerberos)
            {
                return credential.AuthenticationType;
            }
            if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKey))
                return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey;

            if (!string.IsNullOrEmpty(credential.PrivateKey))
            {
                if (SshCaSettings.UseCertificateAuthorityForAuthentication ||
                    credential.AuthenticationType is ClusterAuthenticationCredentialsAuthType.SshCertificate or ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy)
                {
                    return ClusterAuthenticationCredentialsAuthType.SshCertificate;
                }
                else
                {
                    return ClusterAuthenticationCredentialsAuthType.PrivateKey;
                }
            }
                

            if (!string.IsNullOrEmpty(credential.Password))
                switch (cluster.ConnectionProtocol)
                {
                    case ClusterConnectionProtocol.MicrosoftHpcApi:
                        return ClusterAuthenticationCredentialsAuthType.Password;

                    case ClusterConnectionProtocol.Ssh:
                        return ClusterAuthenticationCredentialsAuthType.Password;

                    case ClusterConnectionProtocol.SshInteractive:
                        return ClusterAuthenticationCredentialsAuthType.PasswordInteractive;

                    case ClusterConnectionProtocol.Http:
                    case ClusterConnectionProtocol.Https:
                        return ClusterAuthenticationCredentialsAuthType.Password;

                    default:
                        return ClusterAuthenticationCredentialsAuthType.Password;
                }
        }
        else
        {
            //skip if type is >= 13 - other than classic 
            if (credential.AuthenticationType >= ClusterAuthenticationCredentialsAuthType.Kerberos)
            {
                return credential.AuthenticationType;
            }
            if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKey))
                return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy;
            
            if (!string.IsNullOrEmpty(credential.PrivateKey))
            {
                if (SshCaSettings.UseCertificateAuthorityForAuthentication ||
                    credential.AuthenticationType is ClusterAuthenticationCredentialsAuthType.SshCertificate or ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy)
                {
                    return ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy;
                }
                else
                {
                    return ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy;
                }
            }

            if (!string.IsNullOrEmpty(credential.Password))
                switch (cluster.ConnectionProtocol)
                {
                    case ClusterConnectionProtocol.MicrosoftHpcApi:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;

                    case ClusterConnectionProtocol.Ssh:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;

                    case ClusterConnectionProtocol.SshInteractive:
                        return ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy;

                    case ClusterConnectionProtocol.Http:
                    case ClusterConnectionProtocol.Https:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;

                    default:
                        return ClusterAuthenticationCredentialsAuthType.PasswordViaProxy;
                }
        }

        if (SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            return cluster?.ProxyConnection is null
                ? ClusterAuthenticationCredentialsAuthType.SshCertificate
                : ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy;
        }

        return credential.AuthenticationType == ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
            ? ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
            : ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent;
    }
}
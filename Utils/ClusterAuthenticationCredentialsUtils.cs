using System;
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
        if (credential.AuthenticationType.HasFlag(ClusterAuthenticationCredentialsAuthType.Unknown))
        {
            if (cluster != null && (cluster.SchedulerType & SchedulerType.FirecRestSlurm) == SchedulerType.FirecRestSlurm)
            {
                return ClusterAuthenticationCredentialsAuthType.FirecRestIdpViaExpirio;
            }
        }

        if (credential.AuthenticationType.HasFlag(ClusterAuthenticationCredentialsAuthType.Kerberos))
        {
            return ClusterAuthenticationCredentialsAuthType.Kerberos;
        }

        //TODO: Newly added authentication types must be explicitly supported.
        if (credential.AuthenticationType > ClusterAuthenticationCredentialsAuthType.Kerberos)
        {
            throw new Exception("Unsupported or unknown AuthenticationType.");
        }

        if (!string.IsNullOrEmpty(credential.Password) && !string.IsNullOrEmpty(credential.PrivateKey))
            return ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey;

        if (!string.IsNullOrEmpty(credential.PrivateKey))
        {
            if (SshCaSettings.UseCertificateAuthorityForAuthentication ||
                credential.AuthenticationType.HasFlag(ClusterAuthenticationCredentialsAuthType.SshCertificate))
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

        if (SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            return ClusterAuthenticationCredentialsAuthType.SshCertificate;
        }

        return credential.AuthenticationType.HasFlag(ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
            ? ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
            : ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent;
    }
}
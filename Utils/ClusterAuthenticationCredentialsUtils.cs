using System;
using HEAppE.CertificateGenerator;
using HEAppE.CertificateGenerator.Generators.v2;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using Microsoft.Extensions.Logging;
using SshCaAPI.Configuration;

namespace HEAppE.Utils;

/// <summary>
///     ClusterAuthenticationCredentialsUtils utils
/// </summary>
public static class ClusterAuthenticationCredentialsUtils
{
    public static bool IsValidSshPublicKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key == "Unable to convert")
            return false;

        var trimmed = key.Trim();
        // SSH CA API strictly requires Ed25519 keys ("ssh-ed25519")
        return trimmed.StartsWith("ssh-ed25519");
    }

    public static string EnsureValidPublicKeyForSshCa(ClusterAuthenticationCredentials credentials, ILogger? logger = null)
    {
        if (credentials == null)
            throw new ArgumentNullException(nameof(credentials));

        // 1. If existing PublicKey is a valid Ed25519 key, return it
        if (IsValidSshPublicKey(credentials.PublicKey))
        {
            return credentials.PublicKey;
        }

        // 2. Try to derive public key from PrivateKey (must result in ssh-ed25519)
        if (!string.IsNullOrWhiteSpace(credentials.PrivateKey))
        {
            try
            {
                var derivedKey = SSHGenerator.GetPublicKeyFromPrivateKey(credentials);
                if (derivedKey != null && IsValidSshPublicKey(derivedKey.PublicKeyInAuthorizedKeysFormat))
                {
                    credentials.PublicKey = derivedKey.PublicKeyInAuthorizedKeysFormat;
                    if (!string.IsNullOrEmpty(derivedKey.PublicKeyFingerprint))
                    {
                        credentials.PublicKeyFingerprint = derivedKey.PublicKeyFingerprint;
                    }
                    logger?.LogInformation($"[SshCaUtils] Derived valid Ed25519 public key from private key for user '{credentials.Username}'.");
                    return credentials.PublicKey;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, $"[SshCaUtils] Failed to derive Ed25519 public key from existing private key for user '{credentials.Username}'.");
            }
        }

        // 3. Key is missing, destroyed, or non-Ed25519: Regenerate Ed25519 key pair for SSH CA API
        logger?.LogWarning($"[SshCaUtils] SSH key for user '{credentials.Username}' is missing, destroyed, or non-Ed25519 (PublicKey: '{credentials.PublicKey}'). Regenerating Ed25519 SSH key pair for SSH CA API.");

        var edGenerator = new EdDSACertGeneratorV2();
        var username = !string.IsNullOrWhiteSpace(credentials.Username) ? credentials.Username : "heappe";

        credentials.CipherType = FileTransferCipherType.Ed25519;
        credentials.PrivateKey = edGenerator.ToPrivateKeyInPEM();
        credentials.PublicKey = edGenerator.ToPublicKeyInAuthorizedKeysFormat(username);
        credentials.PublicKeyFingerprint = edGenerator.GetPublicKeyFingerprint();

        logger?.LogInformation($"[SshCaUtils] Successfully regenerated new Ed25519 SSH key pair for user '{credentials.Username}'.");
        return credentials.PublicKey;
    }

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
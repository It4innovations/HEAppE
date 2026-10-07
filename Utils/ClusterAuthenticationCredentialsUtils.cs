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

        // 1. If PrivateKey is present, derive and verify the Ed25519 public key directly from it.
        // This guarantees that the public key sent to SSH CA matches the private key used for SSH authentication.
        if (!string.IsNullOrWhiteSpace(credentials.PrivateKey))
        {
            try
            {
                var derivedKey = EdDSACertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(
                    credentials.PrivateKey, credentials.PrivateKeyPassphrase, credentials.Username);

                if (IsValidSshPublicKey(derivedKey))
                {
                    if (credentials.PublicKey != derivedKey)
                    {
                        logger?.LogInformation($"[SshCaUtils] Synchronizing PublicKey with PrivateKey for user '{credentials.Username}'.");
                        credentials.PublicKey = derivedKey;
                    }
                    credentials.CipherType = FileTransferCipherType.Ed25519;
                    return credentials.PublicKey;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, $"[SshCaUtils] Failed to derive Ed25519 public key from private key for user '{credentials.Username}'.");
            }
        }
        else if (IsValidSshPublicKey(credentials.PublicKey))
        {
            // PrivateKey is not loaded/available in this context, but PublicKey is already a valid Ed25519 key
            return credentials.PublicKey;
        }

        // 2. If PrivateKey is not loaded (Vault down/unavailable) and entity already exists in DB,
        // DO NOT overwrite or regenerate keys - return existing PublicKey if available.
        if (!credentials.IsVaultDataLoaded && credentials.Id > 0 && !string.IsNullOrWhiteSpace(credentials.PublicKey))
        {
            logger?.LogWarning($"[SshCaUtils] Vault data not loaded for existing Credential ID {credentials.Id} ('{credentials.Username}'). Keeping existing PublicKey without regenerating.");
            return credentials.PublicKey;
        }

        // 3. PrivateKey is missing, destroyed, or non-Ed25519 (e.g. RSA or unparseable).
        // Since SSH CA strictly requires Ed25519, non-Ed25519 credentials cannot be certified.
        // Regenerate a fresh matching Ed25519 key pair for SSH CA API.
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
using System;
using System.Reflection;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.CertificateGenerator.Generators;
using HEAppE.CertificateGenerator.Generators.v2;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.Management;
using log4net;

namespace HEAppE.CertificateGenerator;

/// <summary>
///     Generator
/// </summary>
public class SSHGenerator
{
    #region Constructors

    /// <summary>
    ///     Construcotr
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public SSHGenerator()
    {
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        CipherType = CipherGeneratorConfiguration.Type;
        if (CipherGeneratorConfiguration.Type == FileTransferCipherType.Unknown)
        {
            _log.Warn(
                "Wrong fill \"TypeName\" or \"Size\" in \"appsetting.json\" config file. HEAppE uses default algorithm for generating temporary keys RSA (4096)!");
            CipherType = FileTransferCipherType.RSA4096;
        }

        _key = CipherGeneratorConfiguration.Type switch
        {
            FileTransferCipherType.RSA3072 => new RSACertGenerator(3072),
            FileTransferCipherType.RSA4096 => new RSACertGenerator(4096),
            FileTransferCipherType.nistP256 => new ECDsaCertGenerator("nistP256"),
            FileTransferCipherType.nistP521 => new ECDsaCertGenerator("nistP521"),
            FileTransferCipherType.Ed25519 => new EdDSACertGenerator(),
            _ => new RSACertGenerator(4096)
        };

        _certGeneratorV2 = CipherGeneratorConfiguration.Type switch
        {
            FileTransferCipherType.RSA3072 => new RSACertGeneratorV2(3072),
            FileTransferCipherType.RSA4096 => new RSACertGeneratorV2(4096),
            FileTransferCipherType.nistP256 => new ECDsaCertGeneratorV2(256),
            FileTransferCipherType.nistP521 => new ECDsaCertGeneratorV2(521),
            FileTransferCipherType.Ed25519 => new EdDSACertGeneratorV2(),
            _ => new RSACertGeneratorV2(4096)
        };
    }

    #endregion

    #region Properties

    /// <summary>
    ///     File transfer cipher type
    /// </summary>
    public FileTransferCipherType CipherType { get; init; }

    #endregion

    #region Instances

    /// <summary>
    ///     Cipher key
    /// </summary>
    private readonly GenericCertGenerator _key;

    private readonly GenericCertGeneratorV2 _certGeneratorV2;

    /// <summary>
    ///     _logger
    /// </summary>
    private readonly ILog _log;

    #endregion

    #region Methods

    public SecureShellKey GetEncryptedSecureShellKey(string username, string passphrase)
    {
        var key = new SecureShellKey
        {
            Username = username,
            Passphrase = passphrase,
            CipherType = CipherGeneratorConfiguration.Type,
            PrivateKeyPEM = _certGeneratorV2.ToEncryptedPrivateKeyInPEM(passphrase),
            PublicKeyPEM = _certGeneratorV2.ToPublicKeyInPEM(),
            PublicKeyInAuthorizedKeysFormat = _certGeneratorV2.ToPublicKeyInAuthorizedKeysFormat(username),
            PublicKeyFingerprint = _certGeneratorV2.GetPublicKeyFingerprint()
        };
        return key;
    }

    public static SecureShellKey GetPublicKeyFromPrivateKey(ClusterAuthenticationCredentials existingKey)
    {
        switch (CipherGeneratorConfiguration.Type)
        {
            case FileTransferCipherType.nistP256:
            case FileTransferCipherType.nistP521:
                return new SecureShellKey
                {
                    Username = existingKey.Username,
                    CipherType = CipherGeneratorConfiguration.Type,
                    PublicKeyPEM = GenericCertGeneratorV2.ToPublicKeyInPEMFromPrivateKey(existingKey.PrivateKey,
                        existingKey.PrivateKeyPassphrase),
                    PublicKeyInAuthorizedKeysFormat =
                        ECDsaCertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(existingKey.PrivateKey,
                            existingKey.PrivateKeyPassphrase, existingKey.Username)
                };
            case FileTransferCipherType.Ed25519:
                return new SecureShellKey
                {
                    Username = existingKey.Username,
                    CipherType = CipherGeneratorConfiguration.Type,
                    PublicKeyPEM = GenericCertGeneratorV2.ToPublicKeyInPEMFromPrivateKey(existingKey.PrivateKey,
                        existingKey.PrivateKeyPassphrase),
                    PublicKeyInAuthorizedKeysFormat =
                        EdDSACertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(existingKey.PrivateKey,
                            existingKey.PrivateKeyPassphrase, existingKey.Username)
                };
            case FileTransferCipherType.RSA3072:
            case FileTransferCipherType.RSA4096:
            default:
                return new SecureShellKey
                {
                    Username = existingKey.Username,
                    CipherType = CipherGeneratorConfiguration.Type,
                    PublicKeyPEM = GenericCertGeneratorV2.ToPublicKeyInPEMFromPrivateKey(existingKey.PrivateKey,
                        existingKey.PrivateKeyPassphrase),
                    PublicKeyInAuthorizedKeysFormat =
                        RSACertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(existingKey.PrivateKey,
                            existingKey.PrivateKeyPassphrase, existingKey.Username)
                };
        };
    }

    /// <summary>
    ///     Re-Generate key
    /// </summary>
    public void Regenerate()
    {
        _key.Regenerate();
    }

    /// <summary>
    ///     Returns the SSH private key
    /// </summary>
    /// <returns></returns>
    public string ToPrivateKey()
    {
        return _key.ToPrivateKey();
    }

    /// <summary>
    ///     Returns the SSH public key
    /// </summary>
    /// <returns></returns>
    public string ToPublicKey()
    {
        return _key.ToPublicKey();
    }

    /// <summary>
    ///     Returns the SSH public key in PuTTY format
    /// </summary>
    /// <returns></returns>
    public string ToPuTTYPublicKey()
    {
        return _key.ToPuTTYPublicKey();
    }

    #endregion
}
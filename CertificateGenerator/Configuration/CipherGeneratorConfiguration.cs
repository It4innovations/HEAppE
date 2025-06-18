using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.CertificateGenerator.Configuration;

/// <summary>
///     SSH key generator configuration
/// </summary>
public sealed class CipherGeneratorConfiguration
{
    #region Private Members

    /// <summary>
    ///     Set SSH cipher type
    /// </summary>
    private static void SetCipherType()
    {
        _type = (_typeName, Size) switch
        {
            ("RSA", 3072) => FileTransferCipherType.RSA3072,
            ("RSA", 4096) => FileTransferCipherType.RSA4096,
            ("ECDSA", 256) => FileTransferCipherType.nistP256,
            ("ECDSA", 521) => FileTransferCipherType.nistP521,
            ("ED25519", 0) => FileTransferCipherType.Ed25519,
            _ => FileTransferCipherType.Unknown
        };
    }

    #endregion

    #region Instances

    /// <summary>
    ///     SSH cipher type name
    /// </summary>
    private static string _typeName;

    /// <summary>
    ///     SSH cipher type
    /// </summary>
    private static FileTransferCipherType _type;

    #endregion

    #region Properties

    /// <summary>
    ///     SSH cipher type name
    /// </summary>
    public static string TypeName
    {
        get => _typeName;
        set => _typeName = value.ToUpper();
    }

    /// <summary>
    ///     SSH cipher size
    /// </summary>
    public static int Size { get; set; }

    /// <summary>
    ///     SSH cipher type
    /// </summary>
    public static FileTransferCipherType Type
    {
        get
        {
            if (_type == default) SetCipherType();

            return _type;
        }
    }

    #endregion
}
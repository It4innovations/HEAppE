namespace HEAppE.CertificateGenerator.Configuration;

/// <summary>
///     Certificate generator configuration
/// </summary>
public sealed class CertificateGeneratorConfiguration
{
    #region Properties

    /// <summary>
    ///     Directory for generated SSH keys
    /// </summary>
    public static string GeneratedKeysDirectory { get; set; }

    /// <summary>
    ///     Generated SSH keys prefix
    /// </summary>
    public static string GeneratedKeyPrefix { get; set; }

    /// <summary>
    ///     Cipher generation configuration
    /// </summary>
    public static CipherGeneratorConfiguration CipherSettings { get; } = new();

    #endregion
}
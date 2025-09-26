namespace HEAppE.BusinessLogicTier.Configuration;

/// <summary>
///     Business logic configuration
/// </summary>
public sealed class BusinessLogicConfiguration
{
    #region Properties

    /// <summary>
    ///     Account rotation
    /// </summary>
    public static bool SharedAccountsPoolMode { get; set; }

    /// <summary>
    /// Automatic Cluster Account Initialization
    /// </summary>
    public static bool AutomaticClusterAccountInitialization { get; set; } = true;

    /// <summary>
    ///     Limit of generated file transfer key per job
    /// </summary>
    public static int GeneratedFileTransferKeyLimitPerJob { get; set; } = 5;

    /// <summary>
    ///     Validity of temporary transfer keys in hours
    /// </summary>
    public static int ValidityOfTemporaryTransferKeysInHours { get; set; } = 24;

    /// <summary>
    ///     Session expiration in seconds
    /// </summary>
    public static int SessionExpirationInSeconds { get; set; } = 900;

    /// <summary>
    ///     HTTP requeues connection timeout in seconds
    /// </summary>
    public static double HTTPRequestConnectionTimeoutInSeconds { get; set; } = 10;

    #endregion
}
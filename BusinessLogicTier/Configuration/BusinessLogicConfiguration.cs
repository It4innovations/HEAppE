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
    ///     Minimum interval in seconds between two consecutive LastAccessTime writes for the same session.
    ///     Requests within this window skip the DB write, eliminating row-lock contention under concurrent load.
    ///     Default: 60 seconds. Set to 0 to always write (legacy behaviour).
    /// </summary>
    public static int SessionLastAccessTimeUpdateIntervalInSeconds { get; set; } = 60;

    /// <summary>
    ///     HTTP requeues connection timeout in seconds
    /// </summary>
    public static double HTTPRequestConnectionTimeoutInSeconds { get; set; } = 10;
    
    /// <summary>
    ///     Auto initialize project credentials on first use
    /// </summary>
    public static bool AutoInitializeProjectCredentialsOnFirstUse { get; set; } = true;

    #endregion
}
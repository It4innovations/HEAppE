namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     Cluster connection pool configuration
/// </summary>
public sealed class ClusterConnectionPoolConfiguration
{
    #region Properties

    /// <summary>
    ///     Connection pool cleaning interval in seconds
    /// </summary>
    public int ConnectionPoolCleaningInterval { get; set; } = 60;

    /// <summary>
    ///     Connection pool max unused interval in seconds
    /// </summary>
    public int ConnectionPoolMaxUnusedInterval { get; set; } = 1800;

    /// <summary>
    ///     Connection retry attempts
    /// </summary>
    public int ConnectionRetryAttempts { get; set; } = 3;

    /// <summary>
    ///     Connection retry attempts (value in ms)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30000;

    #endregion
}
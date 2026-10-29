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
    public int ConnectionPoolMaxUnusedInterval { get; set; } = 60;

    /// <summary>
    ///     Connection pool maximum connections per user (slots per credential)
    /// </summary>
    public int MaxConnectionsPerUser { get; set; } = 10;

    /// <summary>
    ///     Connection pool maximum sessions per one physical connection (multiplexing limit)
    /// </summary>
    public int MaxSessionsPerConnection { get; set; } = 8;

    /// <summary>
    ///     Maximum time (in milliseconds) to wait for an available connection or session slot.
    ///     Default: 30000ms (30 seconds).
    /// </summary>
    public int ConnectionAcquireTimeoutMs { get; set; } = 30000;

    #endregion
}
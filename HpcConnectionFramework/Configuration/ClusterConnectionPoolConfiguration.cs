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
    ///     Connection pool maximum connections per user (slots per credential)
    /// </summary>
    public int MaxConnectionsPerUser { get; set; } = 10;

    #endregion
}
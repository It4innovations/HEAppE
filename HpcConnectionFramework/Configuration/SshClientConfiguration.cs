namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     Cluster connection pool configuration
/// </summary>
public sealed class SshClientConfiguration
{
    #region Properties
    /// <summary>
    ///     Connection retry attempts
    /// </summary>
    public int ConnectionRetryAttempts { get; set; } = 10;

    /// <summary>
    ///     Connection retry attempts (value in ms)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30000;

    #endregion
}
namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     HPC connection framework configuration
/// </summary>
public sealed class HPCConnectionFrameworkConfiguration
{
    #region Properties

    /// <summary>
    ///     Generic command key parameter
    /// </summary>
    public static string GenericCommandKeyParameter { get; set; }

    /// <summary>
    ///     Database job array delimiter
    /// </summary>
    public static string JobArrayDbDelimiter { get; set; }

    /// <summary>
    ///     Tunnel configuration
    /// </summary>
    public static TunnelConfiguration TunnelSettings { get; } = new();

    /// <summary>
    ///     Clusters connection Pool configuration
    /// </summary>
    public static ClusterConnectionPoolConfiguration ClustersConnectionPoolSettings { get; } = new();

    /// <summary>
    ///     Clusters connection Pool configuration
    /// </summary>
    public static SshClientConfiguration SshClientSettings { get; } = new();

    /// <summary>
    ///     Clusters scripts configuration
    /// </summary>
    public static ScriptsConfiguration ScriptsSettings { get; } = new();

    #endregion
}
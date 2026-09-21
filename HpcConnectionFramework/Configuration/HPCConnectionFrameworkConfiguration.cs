namespace HEAppE.HpcConnectionFramework.Configuration;

using System.Collections.Generic;

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

    /// <summary>
    /// Return full path to execute command script
    /// </summary>
    /// <param name="projectAccountingString"></param>
    /// <returns></returns>
    public static string GetExecuteCmdScriptPath(string projectAccountingString)
    {
        return GetExecuteCmdScriptPath(projectAccountingString, null);
    }

    /// <summary>
    /// Return full path to execute command script
    /// </summary>
    /// <param name="projectAccountingString"></param>
    /// <param name="customConfiguration"></param>
    /// <returns></returns>
    public static string GetExecuteCmdScriptPath(string projectAccountingString, Dictionary<string, string>? customConfiguration)
    {
        var config = ClusterRuntimeConfiguration.For(customConfiguration);
        return config.GetExecuteCmdScriptPath(projectAccountingString);
    }

    /// <summary>
    /// Return full path to script for project
    /// </summary>
    /// <param name="projectAccountingString"></param>
    /// <param name="scriptName"></param>
    /// <returns></returns>
    public static string GetPathToScript(string projectAccountingString, string scriptName)
    {
        return GetPathToScript(projectAccountingString, scriptName, null);
    }

    /// <summary>
    /// Return full path to script for project
    /// </summary>
    /// <param name="projectAccountingString"></param>
    /// <param name="scriptName"></param>
    /// <param name="customConfiguration"></param>
    /// <returns></returns>
    public static string GetPathToScript(string projectAccountingString, string scriptName, Dictionary<string, string>? customConfiguration)
    {
        var config = ClusterRuntimeConfiguration.For(customConfiguration);
        return config.GetPathToScript(projectAccountingString, scriptName);
    }
}
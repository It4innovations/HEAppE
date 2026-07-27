using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     Per-cluster runtime configuration that can override <b>any</b> appsettings value
///     at runtime using entries from <c>Cluster.CustomConfiguration</c>.
///     <para>
///         Keys in <c>Cluster.CustomConfiguration</c> follow the standard .NET configuration
///         colon-separated notation, e.g.:
///         <code>
///             "HPCConnectionFrameworkSettings:ScriptsSettings:ScriptsBasePath" → "/storage/home/user/.HEAppE"
///             "HPCConnectionFrameworkSettings:ScriptsSettings:ClusterScriptsRepository" → "https://other-repo.git"
///             "BusinessLogicSettings:SharedAccountsPoolMode" → "false"
///         </code>
///         Short-form keys (without section prefix) are also accepted for convenience when the
///         full key is unambiguous within the cluster context:
///         <code>
///             "ScriptsBasePath" → "/storage/home/user/.HEAppE"
///         </code>
///         The resolution order is: <b>exact key</b> → <b>short key suffix match</b> → <b>global appsettings value</b>.
///     </para>
///     <para>
///         Because <c>Cluster.CustomConfiguration</c> is loaded fresh from the database on every
///         request, changes take effect immediately — <b>no application restart required</b>.
///     </para>
/// </summary>
public sealed class ClusterRuntimeConfiguration
{
    #region Fields

    /// <summary>Global <see cref="IConfiguration" /> registered in DI, set once at startup.</summary>
    public static IConfiguration GlobalConfiguration { get; set; }

    private readonly IConfiguration _effectiveConfig;
    private readonly IReadOnlyDictionary<string, string> _overrides;

    #endregion

    #region Constructor

    /// <summary>
    ///     Creates a cluster-scoped runtime configuration by layering
    ///     <paramref name="clusterCustomConfig" /> on top of the global <see cref="GlobalConfiguration" />.
    /// </summary>
    /// <param name="clusterCustomConfig">
    ///     <c>Cluster.CustomConfiguration</c> — may be <c>null</c>.
    /// </param>
    public ClusterRuntimeConfiguration(Dictionary<string, string> clusterCustomConfig = null)
    {
        _overrides = clusterCustomConfig ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _effectiveConfig = BuildEffectiveConfiguration(_overrides);
    }

    #endregion

    #region ScriptsConfiguration — convenience properties (resolved via effective config)

    private ScriptsConfiguration _resolvedScripts;

    /// <summary>
    ///     Effective <see cref="ScriptsConfiguration" /> with cluster overrides applied.
    ///     Lazy-bound from <see cref="_effectiveConfig" /> on first access.
    /// </summary>
    public ScriptsConfiguration Scripts
    {
        get
        {
            if (_resolvedScripts is not null) return _resolvedScripts;

            // Start from a clone of the global defaults so unchanged values are preserved.
            _resolvedScripts = new ScriptsConfiguration
            {
                ClusterScriptsRepository    = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepository,
                ClusterScriptsRepositoryBranch = HPCConnectionFrameworkConfiguration.ScriptsSettings.ClusterScriptsRepositoryBranch,
                KeyScriptsDirectoryInRepository = HPCConnectionFrameworkConfiguration.ScriptsSettings.KeyScriptsDirectoryInRepository,
                InstanceIdentifierPath      = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath,
                SubExecutionsPath           = HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath,
                JobLogArchiveSubPath        = HPCConnectionFrameworkConfiguration.ScriptsSettings.JobLogArchiveSubPath,
                SubScriptsPath              = HPCConnectionFrameworkConfiguration.ScriptsSettings.SubScriptsPath,
                ScriptsBasePath             = HPCConnectionFrameworkConfiguration.ScriptsSettings.ScriptsBasePath,
                EnableCallback              = HPCConnectionFrameworkConfiguration.ScriptsSettings.EnableCallback,
                EnableGracefulTimeout       = HPCConnectionFrameworkConfiguration.ScriptsSettings.EnableGracefulTimeout,
                GracefulTimeoutSeconds      = HPCConnectionFrameworkConfiguration.ScriptsSettings.GracefulTimeoutSeconds,
                CallbackUrl                 = HPCConnectionFrameworkConfiguration.ScriptsSettings.CallbackUrl,
                EventualConsistencyRetryCount = HPCConnectionFrameworkConfiguration.ScriptsSettings.EventualConsistencyRetryCount,
                EventualConsistencyRetryDelayMs = HPCConnectionFrameworkConfiguration.ScriptsSettings.EventualConsistencyRetryDelayMs,
                SshCommandPrefix            = HPCConnectionFrameworkConfiguration.ScriptsSettings.SshCommandPrefix,
                SyncScriptsViaSftp          = HPCConnectionFrameworkConfiguration.ScriptsSettings.SyncScriptsViaSftp,
            };

            // Bind override section on top — only keys present in overrides will overwrite.
            _effectiveConfig
                .GetSection("HPCConnectionFrameworkSettings:ScriptsSettings")
                .Bind(_resolvedScripts);

            return _resolvedScripts;
        }
    }

    // Shortcuts for frequently used values
    public string ScriptsBasePath        => Scripts.ScriptsBasePath;
    public string InstanceIdentifierPath => Scripts.InstanceIdentifierPath;
    public string SubExecutionsPath      => Scripts.SubExecutionsPath;
    public string JobLogArchiveSubPath   => Scripts.JobLogArchiveSubPath;
    public CommandScriptPathConfiguration CommandScriptsPathSettings => Scripts.CommandScriptsPathSettings;
    public string? SshCommandPrefix      => Scripts.SshCommandPrefix;
    public bool SyncScriptsViaSftp       => Scripts.SyncScriptsViaSftp;
    public bool EnableCallback          => Scripts.EnableCallback;
    public bool EnableGracefulTimeout   => Scripts.EnableGracefulTimeout;
    public int GracefulTimeoutSeconds   => Scripts.GracefulTimeoutSeconds;

    #endregion

    #region Generic value access

    /// <summary>
    ///     Reads any configuration value by its full colon-separated key,
    ///     with cluster overrides applied on top of the global config.
    /// </summary>
    /// <example>
    ///     <code>
    ///         var path = clusterConfig.GetValue("HPCConnectionFrameworkSettings:ScriptsSettings:ScriptsBasePath");
    ///     </code>
    /// </example>
    public string GetValue(string key) => _effectiveConfig[key];

    /// <summary>
    ///     Binds a configuration section to a new instance of <typeparamref name="T" />
    ///     with cluster overrides applied.
    /// </summary>
    public T GetSection<T>(string sectionKey) where T : new()
    {
        var instance = new T();
        _effectiveConfig.GetSection(sectionKey).Bind(instance);
        return instance;
    }

    #endregion

    #region Path helpers

    /// <summary>
    ///     Expands <c>$USER</c> and <c>${USER}</c> placeholders in <paramref name="path" />
    ///     with the given <paramref name="username" />.
    /// </summary>
    private static string ExpandUser(string path, string username)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(path))
            return path;
        return path.Replace("${USER}", username).Replace("$USER", username);
    }

    /// <summary>
    ///     Full path to a named <c>.key_scripts</c> script for the given project,
    ///     respecting cluster-level overrides.
    ///     <para>
    ///         If <paramref name="username" /> is provided, any <c>$USER</c> / <c>${USER}</c>
    ///         placeholders in <c>ScriptsBasePath</c> are expanded (e.g.
    ///         <c>/storage/brno2/home/$USER/.HEAppE</c> → <c>/storage/brno2/home/kon0379/.HEAppE</c>).
    ///     </para>
    /// </summary>
    public string GetPathToScript(string projectAccountingString, string scriptName, string username = null) =>
        $"{ExpandUser(ScriptsBasePath, username)}/.{projectAccountingString}/{InstanceIdentifierPath}/.key_scripts/{scriptName}";

    /// <summary>
    ///     Full path to the execute-command script for the given project,
    ///     respecting cluster-level overrides.
    ///     <para>
    ///         If <paramref name="username" /> is provided, any <c>$USER</c> / <c>${USER}</c>
    ///         placeholders in <c>ScriptsBasePath</c> are expanded.
    ///     </para>
    /// </summary>
    public string GetExecuteCmdScriptPath(string projectAccountingString, string username = null) =>
        $"{ExpandUser(ScriptsBasePath, username)}/.{projectAccountingString}/{InstanceIdentifierPath}/.key_scripts/{CommandScriptsPathSettings.ExecuteCmdScriptName}";

    #endregion

    #region Factory

    /// <summary>
    ///     Creates a <see cref="ClusterRuntimeConfiguration" /> for the given cluster's
    ///     <c>CustomConfiguration</c>.  Returns a config that uses only global appsettings
    ///     when <paramref name="clusterCustomConfig" /> is <c>null</c> or empty.
    /// </summary>
    public static ClusterRuntimeConfiguration For(Dictionary<string, string> clusterCustomConfig) =>
        new(clusterCustomConfig);

    #endregion

    #region Private

    private static readonly System.Collections.Generic.HashSet<string> KnownBooleanKeys = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "EnableCallback",
        "EnableGracefulTimeout",
        "SyncScriptsViaSftp"
    };

    private static bool IsKnownBooleanKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var keyName = key.Contains(':') ? key.Split(':').Last() : key;
        return KnownBooleanKeys.Contains(keyName);
    }

    /// <summary>
    ///     Builds an <see cref="IConfiguration" /> that stacks the cluster overrides on top of
    ///     the global configuration.  The cluster dictionary is copied verbatim — keys are
    ///     expected in the standard colon-separated .NET notation, but short-form keys
    ///     (without the root section) are also supported for backwards compatibility by
    ///     automatically prefixing them with <c>HPCConnectionFrameworkSettings:ScriptsSettings:</c>.
    /// </summary>
    private static IConfiguration BuildEffectiveConfiguration(IReadOnlyDictionary<string, string> overrides)
    {
        if (overrides.Count == 0 && GlobalConfiguration is not null)
            return GlobalConfiguration;

        // Expand short-form keys to their full path in the config tree.
        const string scriptsPrefix = "HPCConnectionFrameworkSettings:ScriptsSettings:";
        var expanded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in overrides)
        {
            var finalKey = key.Contains(':') ? key : scriptsPrefix + key;
            var val = value;
            if (IsKnownBooleanKey(key) && string.IsNullOrWhiteSpace(val))
            {
                val = "false";
            }
            expanded[finalKey] = val;
        }

        var builder = new ConfigurationBuilder();
        if (GlobalConfiguration is not null)
        {
            builder.AddConfiguration(GlobalConfiguration);          // global appsettings (base layer)
        }

        return builder
            .Add(new MemoryConfigurationSource              // cluster overrides (top layer)
            {
                InitialData = expanded
            })
            .Build();
    }

    #endregion
}

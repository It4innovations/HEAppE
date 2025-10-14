namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     Cluster scripts configuration
/// </summary>
public sealed class ScriptsConfiguration
{
    #region Instances

    private string _instanceIdentifierPath = "Identifier";
    private string _subExecutionPath = "HEAppE/Executions";
    private string _jobLogArchiveSubPath = "HEAppE/JobLogs";
    private string _subScriptsPath = "HEAppE/Scripts";
    private string _scriptsBasePath = "~/.key_scripts";

    #endregion

    #region Properties

    /// <summary>
    ///     Cluster HEAppE Scripts GIT repository URI
    /// </summary>
    public string ClusterScriptsRepository { get; set; }

    public string ClusterScriptsRepositoryBranch { get; set; } = "master";

    /// <summary>
    ///     .key_scripts HEAppE Scripts repository path
    /// </summary>
    public string KeyScriptsDirectoryInRepository { get; set; }

    /// <summary>
    ///     HEAppE Instance Identifier Path
    /// </summary>
    public string InstanceIdentifierPath
    {
        get => _instanceIdentifierPath;
        set
        {
            if (!string.IsNullOrEmpty(value)) _instanceIdentifierPath = value.Replace("\\", "/").TrimStart('/').TrimEnd('/');
        }
    }

    /// <summary>
    ///     Sub Execution Path
    /// </summary>
    public string SubExecutionsPath
    {
        get => _subExecutionPath;
        set
        {
            if (!string.IsNullOrEmpty(value)) _subExecutionPath = value.Replace("\\", "/").TrimStart('/').TrimEnd('/');
        }
    }

    public string JobLogArchiveSubPath
    {
        get => _jobLogArchiveSubPath;
        set
        {
            if (!string.IsNullOrEmpty(value)) _jobLogArchiveSubPath = value.Replace("\\", "/").TrimStart('/').TrimEnd('/');
        }
    }

    /// <summary>
    ///     Sub Script Path
    /// </summary>
    public string SubScriptsPath
    {
        get => _subScriptsPath;
        set
        {
            if (!string.IsNullOrEmpty(value)) _subScriptsPath = value.Replace("\\", "/").TrimStart('/').TrimEnd('/');
        }
    }

    /// <summary>
    ///     Script Base Path
    /// </summary>
    public string ScriptsBasePath
    {
        get => _scriptsBasePath;
        set
        {
            if (!string.IsNullOrEmpty(value)) _scriptsBasePath = value.Replace("\\", "/").TrimEnd('/');
        }
    }

    /// <summary>
    ///     Command scripts path configuration
    /// </summary>
    public CommandScriptPathConfiguration CommandScriptsPathSettings { get; } = new();

    /// <summary>
    ///     Linux local command scripts path configuration
    /// </summary>
    public LinuxLocalCommandScriptPathConfiguration LinuxLocalCommandScriptPathSettings { get; } = new();

    #endregion
}
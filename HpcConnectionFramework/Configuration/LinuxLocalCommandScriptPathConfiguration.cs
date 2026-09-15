namespace HEAppE.HpcConnectionFramework.Configuration;

/// <summary>
///     Cluster scripts used in LINUX local
/// </summary>
public sealed class LinuxLocalCommandScriptPathConfiguration
{
    #region Instances

    private string _scriptsBasePath = "~/.local_hpc_scripts";

    #endregion

    #region Properties

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
    ///     Path to Prepare LocalHPC job directory
    /// </summary>
    public string PrepareJobDirCmdScriptName { get; set; } = "prepare_job_dir.sh";

    /// <summary>
    ///     Run local job execution simulation
    /// </summary>
    public string RunLocalCmdScriptName { get; set; } = "run_local.sh";

    /// <summary>
    ///     Path to execute job info get cmd
    /// </summary>
    public string GetJobInfoCmdScriptName { get; set; } = "get_job_info.sh";

    /// <summary>
    ///     Path to execute count jobs
    /// </summary>
    public string CountJobsCmdScriptName { get; set; } = "count_jobs.sh";

    /// <summary>
    ///     Path to execute cancel simulated job
    /// </summary>
    public string CancelJobCmdScriptName { get; set; } = "cancel_job.sh";

    #endregion
}
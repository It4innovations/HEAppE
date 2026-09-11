using System.ComponentModel;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Source of job/task state update
/// </summary>
[Description("Source of job/task state update")]
public enum JobStateSourceExt
{
    /// <summary>
    /// Unknown / legacy / initial state
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// State was updated via webhook/Slurm callback
    /// </summary>
    Callback = 1,

    /// <summary>
    /// State was directly queried from system on user request
    /// </summary>
    UserVerified = 2,

    /// <summary>
    /// State was updated via background polling service
    /// </summary>
    BackgroundPoll = 3
}

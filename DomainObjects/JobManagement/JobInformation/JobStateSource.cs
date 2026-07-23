namespace HEAppE.DomainObjects.JobManagement.JobInformation;

/// <summary>
///     Source of the job/task state update
/// </summary>
public enum JobStateSource
{
    /// <summary>
    ///     Unknown / legacy / initial state
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     State was updated via webhook/Slurm callback
    /// </summary>
    Callback = 1,

    /// <summary>
    ///     State was directly queried from system on user request
    /// </summary>
    UserVerified = 2,

    /// <summary>
    ///     State was updated via background polling service
    /// </summary>
    BackgroundPoll = 3
}

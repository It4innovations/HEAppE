namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.DTO;

/// <summary>
///     PBS Professional queue allocation information object
/// </summary>
public class PbsProQueueInfo
{
    #region Properties

    /// <summary>
    ///     Queue assigned nodes
    /// </summary>
    [Scheduler("resources_assigned.nodect")]
    public int NodesUsed { get; set; }

    /// <summary>
    ///     Queue priority
    /// </summary>
    [Scheduler("Priority")]
    public int Priority { get; set; }

    /// <summary>
    ///     Queue total jobs
    /// </summary>
    [Scheduler("total_jobs")]
    public int TotalJobs { get; set; }

    #endregion
}
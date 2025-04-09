namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

/// <summary>
///     Scheduler conversion adapter factory
/// </summary>
public abstract class ConversionAdapterFactory
{
    /// <summary>
    ///     Create job adapter
    /// </summary>
    /// <returns></returns>
    public abstract ISchedulerJobAdapter CreateJobAdapter();

    /// <summary>
    ///     Create job task adapter
    /// </summary>
    /// <param name="taskSource">Task source</param>
    /// <returns></returns>
    public abstract ISchedulerTaskAdapter CreateTaskAdapter(object taskSource);
}
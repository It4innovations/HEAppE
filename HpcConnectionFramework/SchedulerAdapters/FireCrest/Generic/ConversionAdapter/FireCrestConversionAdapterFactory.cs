using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic.ConversionAdapter;

/// <summary>
///     FireCrest conversion adapter factory
/// </summary>
public class FireCrestConversionAdapterFactory : ConversionAdapterFactory
{
    /// <summary>
    ///     Create job adapter
    /// </summary>
    /// <returns></returns>
    public override ISchedulerJobAdapter CreateJobAdapter()
    {
        return new FireCrestJobAdapter();
    }

    /// <summary>
    ///     Create job task adapter
    /// </summary>
    /// <param name="taskSource">Task source</param>
    /// <returns></returns>
    public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
    {
        return new FireCrestTaskAdapter((string)taskSource);
    }
}
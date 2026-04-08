using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic.ConversionAdapter;

/// <summary>
///     FirecRest conversion adapter factory
/// </summary>
public class FirecRestConversionAdapterFactory : ConversionAdapterFactory
{
    /// <summary>
    ///     Create job adapter
    /// </summary>
    /// <returns></returns>
    public override ISchedulerJobAdapter CreateJobAdapter()
    {
        return new FirecRestJobAdapter();
    }

    /// <summary>
    ///     Create job task adapter
    /// </summary>
    /// <param name="taskSource">Task source</param>
    /// <returns></returns>
    public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
    {
        return new FirecRestTaskAdapter((string)taskSource);
    }
}
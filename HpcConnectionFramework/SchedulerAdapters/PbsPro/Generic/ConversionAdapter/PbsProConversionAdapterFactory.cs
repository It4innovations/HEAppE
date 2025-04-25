using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

/// <summary>
///     PBS Professional conversion adapter factory
/// </summary>
public class PbsProConversionAdapterFactory : ConversionAdapterFactory
{
    #region ConversionAdapterFactory Members

    /// <summary>
    ///     Create job adapter
    /// </summary>
    /// <returns></returns>
    public override ISchedulerJobAdapter CreateJobAdapter()
    {
        return new PbsProJobAdapter();
    }

    /// <summary>
    ///     Create job task adapter
    /// </summary>
    /// <param name="taskSource">Task source</param>
    /// <returns></returns>
    public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
    {
        return new PbsProTaskAdapter((string)taskSource);
    }

    #endregion
}
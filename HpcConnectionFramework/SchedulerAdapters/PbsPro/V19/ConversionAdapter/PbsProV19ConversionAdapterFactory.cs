using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19.ConversionAdapter
{
    /// <summary>
    /// PbsPro V19 conversion adapter factory
    /// </summary>
    public class PbsProV19ConversionAdapterFactory : PbsProConversionAdapterFactory
    {
        #region ConversionAdapterFactory Members
        /// <summary>
        /// Create job adapter
        /// </summary>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new PbsProV19JobAdapter();
        }

        /// <summary>
        /// Create job task adapter
        /// </summary>
        /// <param name="taskSource">Task source</param>
        /// <returns></returns>
        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new PbsProV19TaskAdapter((string)taskSource);
        }
        #endregion
    }
}
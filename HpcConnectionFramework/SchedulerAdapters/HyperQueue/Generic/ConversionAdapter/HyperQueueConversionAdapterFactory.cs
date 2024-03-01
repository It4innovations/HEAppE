using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic.ConversionAdapter
{
    /// <summary>
    /// HyperQueue conversion adapter factory
    /// </summary>
    public class HyperQueueConversionAdapterFactory : ConversionAdapterFactory
    {
        /// <summary>
        /// Create job adapter
        /// </summary>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new HyperQueueJobAdapter();
        }

        /// <summary>
        /// Create job task adapter
        /// </summary>
        /// <param name="taskSource">Task source</param>
        /// <returns></returns>
        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new HyperQueueTaskAdapter((string)taskSource);
        }
    }
}

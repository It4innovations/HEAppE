using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Slurm conversion adapter factory
    /// </summary>
    public class SlurmConversionAdapterFactory : ConversionAdapterFactory
    {
        /// <summary>
        /// Create job adapter
        /// </summary>
        /// <param name="jobSource">Job source</param>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new SlurmJobAdapter();
        }

        /// <summary>
        /// Create job task adapter
        /// </summary>
        /// <param name="taskSource">Task source</param>
        /// <returns></returns>
        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new SlurmTaskAdapter((string)taskSource);
        }
    }
}

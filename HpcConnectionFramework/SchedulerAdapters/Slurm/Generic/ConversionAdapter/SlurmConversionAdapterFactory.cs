using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Class: Slurm conversion adapter factory
    /// </summary>
    public class SlurmConversionAdapterFactory : ConversionAdapterFactory
    {
        /// <summary>
        /// Method: Create job adapter
        /// </summary>
        /// <param name="jobSource">Job source</param>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new SlurmJobAdapter();
        }

        /// <summary>
        /// Method: Create job adapter
        /// </summary>
        /// <param name="jobSource">Job source</param>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter(object jobSource)
        {
            return new SlurmJobAdapter((string)jobSource);
        }


        /// <summary>
        /// Method: Create job task adapter
        /// </summary>
        /// <param name="taskSource">Task source</param>
        /// <returns></returns>
        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new SlurmTaskAdapter((string)taskSource);
        }
    }
}

using HEAppE.HpcConnectionFramework.ConversionAdapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18.ConversionAdapter
{
    /// <summary>
    /// Class: Slurm conversion adapter factory
    /// </summary>
    public class SlurmV18ConversionAdapterFactory : ConversionAdapterFactory
    {
        /// <summary>
        /// Method: Create job adapter
        /// </summary>
        /// <param name="jobSource">Job source</param>
        /// <returns></returns>
        public override ISchedulerJobAdapter CreateJobAdapter(object jobSource)
        {
            return new SlurmV18JobAdapter((string)jobSource);
        }

        /// <summary>
        /// Method: Create job task adapter
        /// </summary>
        /// <param name="taskSource">Task source</param>
        /// <returns></returns>
        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new SlurmV18TaskAdapter((string)taskSource);
        }
    }
}

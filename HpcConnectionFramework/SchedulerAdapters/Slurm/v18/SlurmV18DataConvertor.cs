using HEAppE.HpcConnectionFramework;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18
{
    /// <summary>
    /// Class: Slurm data convertor
    /// </summary>
    public class SlurmV18DataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
        public SlurmV18DataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }
        #endregion
    }
}

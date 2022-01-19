using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic
{
    /// <summary>
    /// Class: Slurm data convertor
    /// </summary>
    public class SlurmDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
        public SlurmDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }
        #endregion
    }
}

using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19
{
    public class PbsProV19DataConvertor : PbsProDataConvertor
    {
        #region Constructors
        public PbsProV19DataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }
        #endregion
    }
}
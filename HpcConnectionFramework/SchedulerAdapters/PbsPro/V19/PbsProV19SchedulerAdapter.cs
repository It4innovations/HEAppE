using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19
{
    internal class PbsProV19SchedulerAdapter : PbsProSchedulerAdapter
    {
        #region Constructors
        internal PbsProV19SchedulerAdapter(ISchedulerDataConvertor convertor) : base(convertor) 
        {
        }
        #endregion
    }
}
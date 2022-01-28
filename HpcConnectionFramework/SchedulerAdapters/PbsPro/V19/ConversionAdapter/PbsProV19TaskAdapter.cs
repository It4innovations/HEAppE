using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19.ConversionAdapter
{
    /// <summary>
    /// PbsPro V19 task adapter
    /// </summary>
    public class PbsProV19TaskAdapter : PbsProTaskAdapter
    {
        #region Constructors
        public PbsProV19TaskAdapter(string taskSource) : base(taskSource) { }
        #endregion
    }
}
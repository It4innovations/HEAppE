using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19.ConversionAdapter
{
    public class PbsProV19ConversionAdapterFactory : PbsProConversionAdapterFactory
    {
        #region ConversionAdapterFactory Members
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new PbsProV19JobAdapter();
        }

        public override ISchedulerJobAdapter CreateJobAdapter(object jobSource)
        {
            return new PbsProV19JobAdapter(jobSource);
        }

        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new PbsProV19TaskAdapter(taskSource);
        }
        #endregion
    }
}
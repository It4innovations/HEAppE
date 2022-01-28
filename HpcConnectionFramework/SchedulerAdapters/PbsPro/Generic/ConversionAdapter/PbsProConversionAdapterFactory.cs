using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
    public class PbsProConversionAdapterFactory : ConversionAdapterFactory
    {
        #region ConversionAdapterFactory Members
        public override ISchedulerJobAdapter CreateJobAdapter()
        {
            return new PbsProJobAdapter();
        }

        public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource)
        {
            return new PbsProTaskAdapter(taskSource);
        }
        #endregion
    }
}
namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter
{
    public abstract class ConversionAdapterFactory
    {
        public abstract ISchedulerJobAdapter CreateJobAdapter();
        public abstract ISchedulerTaskAdapter CreateTaskAdapter(object taskSource);
    }
}
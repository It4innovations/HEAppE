namespace HEAppE.HpcConnectionFramework.ConversionAdapter {
	public abstract class ConversionAdapterFactory {
		public abstract ISchedulerJobAdapter CreateJobAdapter(object jobSource);
		public abstract ISchedulerTaskAdapter CreateTaskAdapter(object taskSource);
	}
}
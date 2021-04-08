using HaaSMiddleware.HpcConnectionFramework.ConversionAdapter;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc.ConversionAdapter {
	public class WindowsHpcConversionAdapterFactory : ConversionAdapterFactory {
		#region ConversionAdapterFactory Members
		public override ISchedulerJobAdapter CreateJobAdapter(object jobSource) {
			return new WindowsHpcJobAdapter(jobSource);
		}

		public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource) {
			return new WindowsHpcTaskAdapter(taskSource);
		}
		#endregion
	}
}
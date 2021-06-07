using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxLocal.ConversionAdapter {
	public class LinuxLocalConversionAdapterFactory : ConversionAdapterFactory {
		#region ConversionAdapterFactory Members
		public override ISchedulerJobAdapter CreateJobAdapter(object jobSource) {
			return new LinuxLocalJobAdapter(jobSource);
		}

		public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource) {
			return new LinuxLocalTaskAdapter(taskSource);
		}
		#endregion
	}
}
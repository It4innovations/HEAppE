using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v12.ConversionAdapter {
	public class LinuxPbsV12ConversionAdapterFactory : ConversionAdapterFactory {
		#region ConversionAdapterFactory Members
		public override ISchedulerJobAdapter CreateJobAdapter(object jobSource) {
			return new LinuxPbsV12JobAdapter(jobSource);
		}

		public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource) {
			return new LinuxPbsV12TaskAdapter(taskSource);
		}
		#endregion
	}
}
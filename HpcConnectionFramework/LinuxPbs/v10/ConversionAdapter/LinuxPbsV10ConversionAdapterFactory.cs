using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter {
	public class LinuxPbsV10ConversionAdapterFactory : ConversionAdapterFactory {
		#region ConversionAdapterFactory Members
		public override ISchedulerJobAdapter CreateJobAdapter(object jobSource) {
			return new LinuxPbsV10JobAdapter(jobSource);
		}

		public override ISchedulerTaskAdapter CreateTaskAdapter(object taskSource) {
			return new LinuxPbsV10TaskAdapter(taskSource);
		}
		#endregion
	}
}
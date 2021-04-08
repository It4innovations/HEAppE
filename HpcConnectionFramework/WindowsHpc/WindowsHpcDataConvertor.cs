using HaaSMiddleware.HpcConnectionFramework.ConversionAdapter;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc {
	public class WindowsHpcDataConvertor : SchedulerDataConvertor {
		public WindowsHpcDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) {}
	}
}
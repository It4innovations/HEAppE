using System.Collections.Generic;
using HaaSMiddleware.ConnectionPool;
using HaaSMiddleware.DomainObjects.ClusterInformation;
using HaaSMiddleware.HpcConnectionFramework.WindowsHpc.ConversionAdapter;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc {
	public class WindowsHpcSchedulerFactory : SchedulerFactory {
		#region SchedulerFactory Members
		public override IRexScheduler CreateScheduler(Cluster configuration) {
			if (!windowsHpcSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
				windowsHpcSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(
					GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
			return windowsHpcSchedulerSingletons[configuration.MasterNodeName];
		}

		protected override ISchedulerAdapter CreateSchedulerAdapter() {
			if (windowsHpcSchedulerAdapterInstance == null)
				windowsHpcSchedulerAdapterInstance = new WindowsHpcSchedulerAdapter(CreateDataConvertor());
			return windowsHpcSchedulerAdapterInstance;
		}

		protected override ISchedulerDataConvertor CreateDataConvertor() {
			if (windowsHpcConvertorSingleton == null)
				windowsHpcConvertorSingleton = new WindowsHpcDataConvertor(new WindowsHpcConversionAdapterFactory());
			return windowsHpcConvertorSingleton;
		}

		protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration) {
			if (!windowsHpcConnectorSingletons.ContainsKey(configuration.MasterNodeName))
				windowsHpcConnectorSingletons[configuration.MasterNodeName] = new WindowsHpcAPIConnector();
			return windowsHpcConnectorSingletons[configuration.MasterNodeName];
		}
		#endregion

		#region Instance Fields
		private readonly Dictionary<string, IPoolableAdapter> windowsHpcConnectorSingletons =
			new Dictionary<string, IPoolableAdapter>();

		private readonly Dictionary<string, IRexScheduler> windowsHpcSchedulerSingletons = new Dictionary<string, IRexScheduler>();
		private ISchedulerDataConvertor windowsHpcConvertorSingleton;
		private ISchedulerAdapter windowsHpcSchedulerAdapterInstance;
		#endregion
	}
}
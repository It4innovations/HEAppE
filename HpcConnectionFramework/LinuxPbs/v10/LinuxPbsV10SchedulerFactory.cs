using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v10 {
	public class LinuxPbsV10SchedulerFactory : SchedulerFactory {
		#region SchedulerFactory Members
		public override IRexScheduler CreateScheduler(Cluster configuration) {
			if (!linuxPbsSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
				linuxPbsSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(
					GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
			return linuxPbsSchedulerSingletons[configuration.MasterNodeName];
		}

		protected override ISchedulerAdapter CreateSchedulerAdapter() {
			if (linuxPbsSchedulerAdapterInstance == null)
				linuxPbsSchedulerAdapterInstance = new LinuxPbsV10SchedulerAdapter(CreateDataConvertor());
			return linuxPbsSchedulerAdapterInstance;
		}

		protected override ISchedulerDataConvertor CreateDataConvertor() {
			if (linuxPbsConvertorSingleton == null)
				linuxPbsConvertorSingleton = new LinuxPbsV10DataConvertor(new LinuxPbsV10ConversionAdapterFactory());
			return linuxPbsConvertorSingleton;
		}

		protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration) {
			if (!linuxPbsConnectorSingletons.ContainsKey(configuration.MasterNodeName))
				linuxPbsConnectorSingletons[configuration.MasterNodeName] = new SshConnector();
			return linuxPbsConnectorSingletons[configuration.MasterNodeName];
		}
		#endregion

		#region Instance Fields
		private readonly Dictionary<string, IPoolableAdapter> linuxPbsConnectorSingletons = new Dictionary<string, IPoolableAdapter>();
		private readonly Dictionary<string, IRexScheduler> linuxPbsSchedulerSingletons = new Dictionary<string, IRexScheduler>();
		private ISchedulerDataConvertor linuxPbsConvertorSingleton;
		private ISchedulerAdapter linuxPbsSchedulerAdapterInstance;
		#endregion
	}
}
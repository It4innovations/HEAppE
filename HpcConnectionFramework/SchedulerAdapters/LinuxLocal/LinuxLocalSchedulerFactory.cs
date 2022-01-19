using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro
{
    public class LinuxLocalSchedulerFactory : SchedulerFactory {
		#region SchedulerFactory Members
		public override IRexScheduler CreateScheduler(Cluster configuration) {
			if (!linuxSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
				linuxSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(
					GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
			return linuxSchedulerSingletons[configuration.MasterNodeName];
		}

		protected override ISchedulerAdapter CreateSchedulerAdapter() {
			if (linuxSchedulerAdapterInstance == null)
				linuxSchedulerAdapterInstance = new LinuxLocalSchedulerAdapter(CreateDataConvertor());
			return linuxSchedulerAdapterInstance;
		}

		protected override ISchedulerDataConvertor CreateDataConvertor() {
			if (linuxConvertorSingleton == null)
				linuxConvertorSingleton = new LinuxLocalDataConvertor();
			return linuxConvertorSingleton;
		}

		protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration) {
			if (!linuxConnectorSingletons.ContainsKey(configuration.MasterNodeName))
				linuxConnectorSingletons[configuration.MasterNodeName] = new SshConnector();
			return linuxConnectorSingletons[configuration.MasterNodeName];
		}
        #endregion
        #region Instance Fields
        private readonly Dictionary<string, IPoolableAdapter> linuxConnectorSingletons = new Dictionary<string, IPoolableAdapter>();
		private readonly Dictionary<string, IRexScheduler> linuxSchedulerSingletons = new Dictionary<string, IRexScheduler>();
		private ISchedulerDataConvertor linuxConvertorSingleton;
		private ISchedulerAdapter linuxSchedulerAdapterInstance;
        #endregion
    }
}
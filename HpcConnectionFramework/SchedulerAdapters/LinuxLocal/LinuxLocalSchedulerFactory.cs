using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
//using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.ConversionAdapter;
using HEAppE.HpcConnectionFramework.LinuxPbs.v12.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal {
	public class LinuxLocalSchedulerFactory : SchedulerFactory {
		#region SchedulerFactory Members
		public override IRexScheduler CreateScheduler(Cluster configuration) {
			if (!linuxPbsSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
				linuxPbsSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(
					GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
			return linuxPbsSchedulerSingletons[configuration.MasterNodeName];
		}

		protected override ISchedulerAdapter CreateSchedulerAdapter() {
			if (linuxPbsSchedulerAdapterInstance == null)
				linuxPbsSchedulerAdapterInstance = new LinuxLocalSchedulerAdapter(CreateDataConvertor());
			return linuxPbsSchedulerAdapterInstance;
		}

		protected override ISchedulerDataConvertor CreateDataConvertor() {
			if (linuxPbsConvertorSingleton == null)
				linuxPbsConvertorSingleton = new LinuxLocalDataConvertor();
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
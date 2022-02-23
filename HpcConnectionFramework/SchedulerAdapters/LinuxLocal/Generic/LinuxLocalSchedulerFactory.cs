using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal
{
    public class LinuxLocalSchedulerFactory : SchedulerFactory 
	{
		#region Instances
		private readonly Dictionary<string, IPoolableAdapter> _linuxConnectorSingletons = new ();
		private readonly Dictionary<string, IRexScheduler> _linuxSchedulerSingletons = new ();
		private ISchedulerDataConvertor _linuxConvertorSingleton;
		private ISchedulerAdapter _linuxSchedulerAdapterInstance;
		#endregion
		#region SchedulerFactory Members
		public override IRexScheduler CreateScheduler(Cluster configuration)
		{
			if (!_linuxSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
            {
				_linuxSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
			}			
			return _linuxSchedulerSingletons[configuration.MasterNodeName];
		}

		protected override ISchedulerAdapter CreateSchedulerAdapter()
		{
			if (_linuxSchedulerAdapterInstance == null)
            {
				_linuxSchedulerAdapterInstance = new LinuxLocalSchedulerAdapter(CreateDataConvertor());
			}	
			return _linuxSchedulerAdapterInstance;
		}

		protected override ISchedulerDataConvertor CreateDataConvertor()
		{
			if (_linuxConvertorSingleton == null)
            {
				_linuxConvertorSingleton = new LinuxLocalDataConvertor();
			}			
			return _linuxConvertorSingleton;
		}

		protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration) 
		{
			if (!_linuxConnectorSingletons.ContainsKey(configuration.MasterNodeName))
            {
				_linuxConnectorSingletons[configuration.MasterNodeName] = new SshConnector();
			}
			return _linuxConnectorSingletons[configuration.MasterNodeName];
		}
        #endregion
    }
}
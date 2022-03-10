using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal
{
    /// <summary>
    /// Local Linux Scheduler Factory
    /// </summary>
    public class LinuxLocalSchedulerFactory : SchedulerFactory
    {
        #region Instances
        /// <summary>
        /// Connector singletons
        /// </summary>
        private readonly Dictionary<string, IPoolableAdapter> _linuxConnectorSingletons = new();
        /// <summary>
        /// Scheduler singletons
        /// </summary>
        private readonly Dictionary<string, IRexScheduler> _linuxSchedulerSingletons = new();
        /// <summary>
        /// Convertor singletons
        /// </summary>
        private readonly ISchedulerDataConvertor _linuxConvertorSingleton;
        /// <summary>
        /// Scheduler adapter singletons
        /// </summary>
        private readonly ISchedulerAdapter _linuxSchedulerAdapterInstance;
        #endregion
        #region SchedulerFactory Members
        /// <summary>
        /// Create scheduler
        /// </summary>
        /// <param name="configuration">Cluster</param>
        /// <returns></returns>
        public override IRexScheduler CreateScheduler(Cluster configuration)
        {
            if (!_linuxSchedulerSingletons.ContainsKey(configuration.MasterNodeName))
            {
                _linuxSchedulerSingletons[configuration.MasterNodeName] = new RexSchedulerWrapper(GetSchedulerConnectionPool(configuration), CreateSchedulerAdapter());
            }
            return _linuxSchedulerSingletons[configuration.MasterNodeName];
        }
        /// <summary>
        /// Create scheduler adapter
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerAdapter CreateSchedulerAdapter()
        {
            return _linuxSchedulerAdapterInstance ?? new LinuxLocalSchedulerAdapter(CreateDataConvertor()); ;
        }

        /// <summary>
        /// Create data convertor
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerDataConvertor CreateDataConvertor()
        {
            return _linuxConvertorSingleton ?? new LinuxLocalDataConvertor();
        }
        /// <summary>
        /// Create scheduler connector
        /// </summary>
        /// <param name="configuration">Cluster</param>
        /// <returns></returns>
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
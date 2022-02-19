using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    /// <summary>
    /// PbsPro scheduler factory
    /// </summary>
    public class PbsProSchedulerFactory : SchedulerFactory
    {
        #region Instances
        /// <summary>
        /// Connectors
        /// </summary>
        private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new();

        /// <summary>
        /// Scheduler singeltons
        /// </summary>
        private readonly Dictionary<string, IRexScheduler> _schedulerSingletons = new();

        /// <summary>
        /// Convertor
        /// </summary>
        private ISchedulerDataConvertor _convertorSingleton;

        /// <summary>
        /// Scheduler adapter
        /// </summary>
        private ISchedulerAdapter _schedulerAdapterInstance;
        #endregion
        #region SchedulerFactory Members
        /// <summary>
        /// Create scheduler
        /// </summary>
        /// <param name="configuration">Cluster configuration data</param>
        /// <returns></returns>
        public override IRexScheduler CreateScheduler(Cluster configuration)
        {
            var masterNodeName = configuration.MasterNodeName;
            if (!_schedulerSingletons.ContainsKey(masterNodeName))
            {
                _schedulerSingletons[masterNodeName] = new RexSchedulerWrapper
                                                       (
                                                            GetSchedulerConnectionPool(configuration),
                                                            CreateSchedulerAdapter()
                                                       );
            }
            return _schedulerSingletons[masterNodeName];
        }

        /// <summary>
        /// Create scheduler adapter
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerAdapter CreateSchedulerAdapter()
        {
            return _schedulerAdapterInstance ??= new PbsProSchedulerAdapter(CreateDataConvertor());
        }

        /// <summary>
        /// Create data convertor
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerDataConvertor CreateDataConvertor()
        {
            return _convertorSingleton ??= new PbsProDataConvertor(new PbsProConversionAdapterFactory());
        }

        /// <summary>
        /// Create scheduler connector
        /// </summary>
        /// <param name="configuration">Cluster configuration data</param>
        /// <returns></returns>
        protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration)
        {
            var masterNodeName = configuration.MasterNodeName;
            if (!_connectorSingletons.ContainsKey(masterNodeName))
            {
                _connectorSingletons[masterNodeName] = new SshConnector();
            }

            return _connectorSingletons[masterNodeName];
        }
        #endregion
    }
}
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18
{
    /// <summary>
    /// Class: Slurm scheduler factory
    /// </summary>
    internal class SlurmV18SchedulerFactory : SchedulerFactory
    {
        #region Properties
        /// <summary>
        /// Connectors
        /// </summary>
        private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new Dictionary<string, IPoolableAdapter>();

        /// <summary>
        /// Scheduler singeltons
        /// </summary>
        private readonly Dictionary<string, IRexScheduler> _schedulerSingletons = new Dictionary<string, IRexScheduler>();

        /// <summary>
        /// Convertor
        /// </summary>
        private ISchedulerDataConvertor _convertorSingleton;

        /// <summary>
        /// Scheduler adapter
        /// </summary>
        private ISchedulerAdapter _schedulerAdapterInstance;
        #endregion
        #region Override methods
        /// <summary>
        /// Method: Create scheduler
        /// </summary>
        /// <param name="configuration">Cluster configuration data</param>
        /// <returns></returns>
        public override IRexScheduler CreateScheduler(Cluster configuration)
        {
            var masterNodeName = configuration.MasterNodeName;
            if (!_schedulerSingletons.ContainsKey(masterNodeName))
            {
                _schedulerSingletons[masterNodeName] = new RexSchedulerWrapper(
                        GetSchedulerConnectionPool(configuration),
                        CreateSchedulerAdapter()
                    );
            }
            return _schedulerSingletons[masterNodeName];
        }

        /// <summary>
        /// Method: Create scheduler adapter
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerAdapter CreateSchedulerAdapter()
        {
            return _schedulerAdapterInstance ?? (_schedulerAdapterInstance = new SlurmV18SchedulerAdapter(CreateDataConvertor()));
        }

        /// <summary>
        /// Method: Create data convertor
        /// </summary>
        /// <returns></returns>
        protected override ISchedulerDataConvertor CreateDataConvertor()
        {
            return _convertorSingleton ?? (_convertorSingleton = new SlurmV18DataConvertor(new SlurmV18ConversionAdapterFactory()));
        }

        /// <summary>
        /// Method: Create scheduler connector
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

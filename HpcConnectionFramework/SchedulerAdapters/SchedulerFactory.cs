using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;
using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    public abstract class SchedulerFactory
    {
        #region Instances
        private readonly Dictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
        private readonly static Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new();
        private static readonly Dictionary<string, ClusterConnectionPoolConfiguration> _connectionPoolSettings = HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;
        #endregion
        #region Static Methods
        public static SchedulerFactory GetInstance(SchedulerType type)
        {
            if (_schedulerFactoryPoolSingletons.ContainsKey(type))
            {
                return _schedulerFactoryPoolSingletons[type];
            }
            else
            {
                SchedulerFactory factoryInstance = type switch
                {
                    SchedulerType.PbsPro => new PbsProSchedulerFactory(),
                    SchedulerType.Slurm => new SlurmSchedulerFactory(),
                    SchedulerType.LinuxLocal => new LinuxLocalSchedulerFactory(),
                    _ => throw new ApplicationException("Scheduler factory with type \"" + type + "\" does not exist."),
                };
                _schedulerFactoryPoolSingletons.Add(type, factoryInstance);
                return factoryInstance;
            }
        }
        #endregion
        #region Abstract Methods
        public abstract IRexScheduler CreateScheduler(Cluster configuration);

        protected abstract ISchedulerAdapter CreateSchedulerAdapter();

        protected abstract ISchedulerDataConvertor CreateDataConvertor();

        protected abstract IPoolableAdapter CreateSchedulerConnector(Cluster configuration);
        #endregion
        #region Local Methods
        protected IConnectionPool GetSchedulerConnectionPool(Cluster clusterConf)
        {
            var endpoint = new SchedulerEndpoint(clusterConf.MasterNodeName, clusterConf.SchedulerType);
            if (!_schedulerConnectionPoolSingletons.ContainsKey(endpoint))
            {
                int connectionPoolMinSize = 0;
                int connectionPoolMaxSize = 5;
                int connectionPoolCleaningInterval = 60;
                int connectionPoolMaxUnusedInterval = 1800;

                if (HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings.ContainsKey(clusterConf.MasterNodeName))
                {
                    connectionPoolMinSize = _connectionPoolSettings[clusterConf.MasterNodeName].ConnectionPoolMinSize;
                    connectionPoolMaxSize = _connectionPoolSettings[clusterConf.MasterNodeName].ConnectionPoolMaxSize;
                    connectionPoolCleaningInterval = _connectionPoolSettings[clusterConf.MasterNodeName].ConnectionPoolCleaningInterval;
                    connectionPoolMaxUnusedInterval = _connectionPoolSettings[clusterConf.MasterNodeName].ConnectionPoolMaxUnusedInterval;
                }

                _schedulerConnectionPoolSingletons[endpoint] = new ConnectionPool.ConnectionPool(
                            clusterConf.MasterNodeName,
                            clusterConf.TimeZone,
                            connectionPoolMinSize,
                            connectionPoolMaxSize,
                            connectionPoolCleaningInterval,
                            connectionPoolMaxUnusedInterval,
                            CreateSchedulerConnector(clusterConf),
                            clusterConf.Port);
            }
            return _schedulerConnectionPoolSingletons[endpoint];
        }
        #endregion
    }
}
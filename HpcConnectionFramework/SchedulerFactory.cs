using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19;

namespace HEAppE.HpcConnectionFramework
{
    public abstract class SchedulerFactory
    {
        #region Instances
        private readonly Dictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
        private readonly static Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new ();

        //TODO add to settings
        private static readonly int ConnectionPoolMinSize = 0;
        private static readonly int ConnectionPoolMaxSize = 10;
        private static readonly int ConnectionPoolCleaningInterval = 60;
        private static readonly int ConnectionPoolMaxUnusedInterval = 1800;
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
                    SchedulerType.PbsProV19 => new PbsProV19SchedulerFactory(),
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
        protected IConnectionPool GetSchedulerConnectionPool(Cluster configuration)
        {
            var endpoint = new SchedulerEndpoint(configuration.MasterNodeName, configuration.SchedulerType);
            if (!_schedulerConnectionPoolSingletons.ContainsKey(endpoint))
            {
                _schedulerConnectionPoolSingletons[endpoint] = new ConnectionPool.ConnectionPool(
                    configuration.MasterNodeName,
                    configuration.TimeZone,
                    ConnectionPoolMinSize,
                    ConnectionPoolMaxSize,
                    ConnectionPoolCleaningInterval,
                    ConnectionPoolMaxUnusedInterval,
                    CreateSchedulerConnector(configuration),
                    configuration.Port);
            }
            return _schedulerConnectionPoolSingletons[endpoint];
        }
        #endregion
    }
}
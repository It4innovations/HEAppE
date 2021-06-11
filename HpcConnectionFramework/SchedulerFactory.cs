using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.LinuxLocal;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10;
using HEAppE.HpcConnectionFramework.LinuxPbs.v12;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18;

namespace HEAppE.HpcConnectionFramework
{
    public abstract class SchedulerFactory
    {
        #region Instantiation
        //TODO add to settings
        private static readonly int ConnectionPoolMinSize = 0;
        private static readonly int ConnectionPoolMaxSize = 10;
        private static readonly int ConnectionPoolCleaningInterval = 60;
        private static readonly int ConnectionPoolMaxUnusedInterval = 1800;

        public static SchedulerFactory GetInstance(SchedulerType type)
        {
            if (schedulerFactoryPoolSingletons.ContainsKey(type))
            {
                return schedulerFactoryPoolSingletons[type];
            }
            else
            {
                SchedulerFactory factoryInstance;
                switch (type)
                {
                    case SchedulerType.LinuxPbsProV10:
                        factoryInstance = new LinuxPbsV10SchedulerFactory();
                        break;
                    case SchedulerType.LinuxPbsProV12:
                        factoryInstance = new LinuxPbsV12SchedulerFactory();
                        break;
                    case SchedulerType.LinuxSlurmV18:
                        factoryInstance = new SlurmV18SchedulerFactory();
                        break;
                    case SchedulerType.LinuxLocal:
                        factoryInstance = new LinuxLocalSchedulerFactory();
                        break;
                    default:
                        throw new ApplicationException("Scheduler factory with type \"" + type + "\" does not exist.");
                }
                //factoryInstance = new LinuxLocalSchedulerFactory();
                schedulerFactoryPoolSingletons.Add(type, factoryInstance);
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
            SchedulerEndpoint endpoint = new SchedulerEndpoint(configuration.MasterNodeName,
                configuration.SchedulerType);
            if (!schedulerConnectionPoolSingletons.ContainsKey(endpoint))
            {
                schedulerConnectionPoolSingletons[endpoint] = new ConnectionPool.ConnectionPool(
                    configuration.MasterNodeName,
                    configuration.TimeZone,
                    ConnectionPoolMinSize,
                    ConnectionPoolMaxSize,
                    ConnectionPoolCleaningInterval,
                    ConnectionPoolMaxUnusedInterval,
                    CreateSchedulerConnector(configuration));
            }
            return schedulerConnectionPoolSingletons[endpoint];
        }
        #endregion

        #region Instance Fields
        private readonly Dictionary<SchedulerEndpoint, IConnectionPool> schedulerConnectionPoolSingletons = new Dictionary<SchedulerEndpoint, IConnectionPool>();
        private readonly static Dictionary<SchedulerType, SchedulerFactory> schedulerFactoryPoolSingletons = new Dictionary<SchedulerType, SchedulerFactory>();
        #endregion
    }
}
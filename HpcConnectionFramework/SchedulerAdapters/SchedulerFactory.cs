using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    /// <summary>
    /// Scheduler factory
    /// </summary>
    public abstract class SchedulerFactory
    {
        #region Instances
        private readonly Dictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
        private readonly static Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new();
        private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings = HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;
        #endregion
        #region Static Methods
        /// <summary>
        /// Get specific instance
        /// </summary>
        /// <param name="type">Instance type</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
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

        /// <summary>
        /// Create scheduler
        /// </summary>
        /// <param name="configuration">Cluster configuration</param>
        /// <param name="jobInfoProject"></param>
        /// <returns></returns>
        public abstract IRexScheduler CreateScheduler(Cluster configuration, Project project);

        /// <summary>
        /// Create scheduler adapter
        /// </summary>
        /// <returns></returns>
        protected abstract ISchedulerAdapter CreateSchedulerAdapter();

        /// <summary>
        /// Create data convertor
        /// </summary>
        /// <returns></returns>
        protected abstract ISchedulerDataConvertor CreateDataConvertor();

        /// <summary>
        /// Create scheduler connector
        /// </summary>
        /// <param name="configuration">Cluster configuration</param>
        /// <returns></returns>
        protected abstract IPoolableAdapter CreateSchedulerConnector(Cluster configuration);
        #endregion
        #region Local Methods
        /// <summary>
        /// Get scheduler connection pool
        /// </summary>
        /// <param name="clusterConf">Cluster configuration</param>
        /// <param name="project">Project</param>
        /// <returns></returns>
        protected IConnectionPool GetSchedulerConnectionPool(Cluster clusterConf, Project project)
        {
            var endpoint = new SchedulerEndpoint(clusterConf.MasterNodeName, project.Id, project.ModifiedAt, clusterConf.SchedulerType);
            if (!_schedulerConnectionPoolSingletons.ContainsKey(endpoint))
            {

                int connectionPoolCleaningInterval = 60;
                int connectionPoolMaxUnusedInterval = 1800;
                
                connectionPoolCleaningInterval = _connectionPoolSettings.ConnectionPoolCleaningInterval;
                connectionPoolMaxUnusedInterval = _connectionPoolSettings.ConnectionPoolMaxUnusedInterval;

                var clusterProject = project.ClusterProjects.FirstOrDefault(x => x.ClusterId == clusterConf.Id);
                if (clusterProject is null || !clusterProject.IsDeleted)
                {
                    throw new ArgumentException(
                        $"Project with ID '{project.Id}' is not referenced to the cluster with ID '{clusterConf.Id}'.");
                }
                
                int connectionPoolMinSize = 0;
                int connectionPoolMaxSize = clusterProject.ClusterProjectCredentials.Count;

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
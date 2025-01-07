using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters;

/// <summary>
///     Scheduler factory
/// </summary>
public abstract class SchedulerFactory
{
    #region Static Methods

    /// <summary>
    ///     Get specific instance
    /// </summary>
    /// <param name="type">Instance type</param>
    /// <returns></returns>
    /// <exception cref="SchedulerException"></exception>
    public static SchedulerFactory GetInstance(SchedulerType type)
    {
        lock (_schedulerFactoryPoolSingletons)
        {
            if (_schedulerFactoryPoolSingletons.ContainsKey(type)) return _schedulerFactoryPoolSingletons[type];

            SchedulerFactory factoryInstance = type switch
            {
                SchedulerType.PbsPro => new PbsProSchedulerFactory(),
                SchedulerType.Slurm => new SlurmSchedulerFactory(),
                SchedulerType.LinuxLocal => new LinuxLocalSchedulerFactory(),
                SchedulerType.HyperQueue => new HyperQueueSchedulerFactory(),
                _ => throw new SchedulerException("NotValidType", type)
            };
            _schedulerFactoryPoolSingletons.Add(type, factoryInstance);
            return factoryInstance;
        }
    }

    #endregion

    #region Local Methods

    /// <summary>
    ///     Get scheduler connection pool
    /// </summary>
    /// <param name="clusterConf">Cluster configuration</param>
    /// <param name="project">Project</param>
    /// <returns></returns>
    protected IConnectionPool GetSchedulerConnectionPool(Cluster clusterConf, Project project)
    {
        var endpoint = new SchedulerEndpoint(clusterConf.MasterNodeName, project.Id, project.ModifiedAt,
            clusterConf.SchedulerType);
        if (!_schedulerConnectionPoolSingletons.ContainsKey(endpoint))
        {
            var connectionPoolCleaningInterval = _connectionPoolSettings.ConnectionPoolCleaningInterval;
            var connectionPoolMaxUnusedInterval = _connectionPoolSettings.ConnectionPoolMaxUnusedInterval;

            var clusterProject = project.ClusterProjects.FirstOrDefault(x => x.ClusterId == clusterConf.Id);
            if (clusterProject is null)
                throw new ArgumentException(
                    $"Project with ID '{project.Id}' is not referenced to the cluster with ID '{clusterConf.Id}'.");

            var connectionPoolMinSize = 0;
            var connectionPoolMaxSize = clusterProject.ClusterProjectCredentials.Count;

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

    #region Instances

    private readonly Dictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
    private static readonly Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new();

    private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings =
        HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;

    #endregion

    #region Abstract Methods

    /// <summary>
    ///     Create scheduler
    /// </summary>
    /// <param name="configuration">Cluster configuration</param>
    /// <param name="jobInfoProject"></param>
    /// <returns></returns>
    public abstract IRexScheduler CreateScheduler(Cluster configuration, Project project);

    /// <summary>
    ///     Create scheduler adapter
    /// </summary>
    /// <returns></returns>
    protected abstract ISchedulerAdapter CreateSchedulerAdapter();

    /// <summary>
    ///     Create data convertor
    /// </summary>
    /// <returns></returns>
    protected abstract ISchedulerDataConvertor CreateDataConvertor();

    /// <summary>
    ///     Create scheduler connector
    /// </summary>
    /// <param name="configuration">Cluster configuration</param>
    /// <returns></returns>
    protected abstract IPoolableAdapter CreateSchedulerConnector(Cluster configuration);

    #endregion
}
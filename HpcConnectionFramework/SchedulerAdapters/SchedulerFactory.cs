using System;
using System.Collections.Concurrent; // NOVÉ: Pro ConcurrentDictionary
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
using SshCaAPI;

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
    public static SchedulerFactory GetInstance(SchedulerType type)
    {
        // Tato statická metoda je již chráněna pomocí lock (_schedulerFactoryPoolSingletons), je Thread-Safe.
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
    protected IConnectionPool GetSchedulerConnectionPool(Cluster clusterConf, Project project, ISshCertificateAuthorityService sshCertificateAuthorityService,long? adaptorUserId)
    {
        if (!project.IsOneToOneMapping)
            adaptorUserId = null;
            
        var endpoint = new SchedulerEndpoint(clusterConf.MasterNodeName, project.Id, project.ModifiedAt,
            clusterConf.SchedulerType, adaptorUserId);

        // OPRAVA: Použití GetOrAdd pro atomické získání/vytvoření ConnectionPoolu
        return _schedulerConnectionPoolSingletons.GetOrAdd(
            endpoint,
            key => 
            {
                var connectionPoolCleaningInterval = _connectionPoolSettings.ConnectionPoolCleaningInterval;
                var connectionPoolMaxUnusedInterval = _connectionPoolSettings.ConnectionPoolMaxUnusedInterval;

                var clusterProject = project.ClusterProjects.FirstOrDefault(x => x.ClusterId == clusterConf.Id);
                if (clusterProject is null)
                    throw new ArgumentException(
                        $"Project with ID '{project.Id}' is not referenced to the cluster with ID '{clusterConf.Id}'.");

                var connectionPoolMinSize = 0;
                var connectionPoolMaxSize = clusterProject.ClusterProjectCredentials.Count;
                
                if (adaptorUserId != null)
                {
                    // Použijeme AdaptorUserId z klíče
                    var currentAdaptorUserId = key.AdaptorUserId;
                    
                    connectionPoolMaxSize = clusterProject.ClusterProjectCredentials
                        .Count(cpc => currentAdaptorUserId.HasValue ? cpc.AdaptorUserId == currentAdaptorUserId : cpc.AdaptorUserId == null);

                    if (connectionPoolMaxSize == 0)
                        throw new SchedulerException($"There are no credentials for 1:1 user mapping for this user.");
                }

                // Vytvoření nové instance ConnectionPool.ConnectionPool
                return new ConnectionPool.ConnectionPool(
                    clusterConf.MasterNodeName,
                    clusterConf.TimeZone,
                    connectionPoolMinSize,
                    connectionPoolMaxSize,
                    connectionPoolCleaningInterval,
                    connectionPoolMaxUnusedInterval,
                    CreateSchedulerConnector(clusterConf, sshCertificateAuthorityService),
                    clusterConf.Port);
            });
    }

    #endregion

    #region Instances

    // OPRAVA: Změněno z Dictionary na ConcurrentDictionary pro bezpečné použití v GetSchedulerConnectionPool
    private readonly ConcurrentDictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
    
    // Zůstává Dictionary, chráněno lockem ve statické metodě GetInstance
    private static readonly Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new();

    private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings =
        HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;

    #endregion

    #region Abstract Methods

    /// <summary>
    ///     Create scheduler
    /// </summary>
    public abstract IRexScheduler CreateScheduler(Cluster configuration, Project project, ISshCertificateAuthorityService sshCertificateAuthorityService, long? adaptorUserId);

    /// <summary>
    ///     Create scheduler adapter
    /// </summary>
    protected abstract ISchedulerAdapter CreateSchedulerAdapter();

    /// <summary>
    ///     Create data convertor
    /// </summary>
    protected abstract ISchedulerDataConvertor CreateDataConvertor();

    /// <summary>
    ///     Create scheduler connector
    /// </summary>
    protected abstract IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService);

    #endregion
}
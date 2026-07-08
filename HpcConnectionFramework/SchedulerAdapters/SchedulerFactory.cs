using System;
using System.Collections.Concurrent; // NOVÉ: Pro ConcurrentDictionary
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;
using HEAppE.Services.Expirio;
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
        lock (_schedulerFactoryPoolSingletons)
        {
            if (_schedulerFactoryPoolSingletons.ContainsKey(type))
                return _schedulerFactoryPoolSingletons[type];

            SchedulerFactory factoryInstance = type switch
            {
                SchedulerType.FirecRestSlurm => new FirecRestSchedulerFactory(type),
                SchedulerType.PbsPro => new PbsProSchedulerFactory(),
                SchedulerType.Slurm => new SlurmSchedulerFactory(),
                SchedulerType.LinuxLocal => new LinuxLocalSchedulerFactory(),
                SchedulerType.HyperQueue => new HyperQueueSchedulerFactory(),
                SchedulerType.QScheduler => new QSchedulerSchedulerFactory(),
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
    protected IConnectionPool GetSchedulerConnectionPool(
        Cluster clusterConf, 
        Project project, 
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        long? adaptorUserId,
        IExpirioService expirio, ILogger logger)
    {
        if (!project.IsOneToOneMapping)
            adaptorUserId = null;
            
        var endpoint = new SchedulerEndpoint(clusterConf.MasterNodeName, project.Id, project.ModifiedAt,
            clusterConf.SchedulerType, adaptorUserId, clusterConf.ProxyConnectionId);

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
                var connectionPoolMaxSize = _connectionPoolSettings.MaxConnectionsPerUser;
                var connectionPoolMaxSessions = _connectionPoolSettings.MaxSessionsPerConnection;
                
                if (adaptorUserId != null)
                {
                    var currentAdaptorUserId = key.AdaptorUserId;
                    
                    var hasCredentials = clusterProject.ClusterProjectCredentials
                        .Any(cpc => currentAdaptorUserId.HasValue ? cpc.AdaptorUserId == currentAdaptorUserId : cpc.AdaptorUserId == null);

                    if (!hasCredentials)
                        throw new SchedulerException("NoOneToOneCredentials");
                }
                
                return new ConnectionPool.ConnectionPool(
                    clusterConf.MasterNodeName,
                    clusterConf.TimeZone,
                    connectionPoolMinSize,
                    connectionPoolMaxSize,
                    connectionPoolMaxSessions,
                    connectionPoolCleaningInterval,
                    connectionPoolMaxUnusedInterval,
                    CreateSchedulerConnector(clusterConf, sshCertificateAuthorityService, expirio, logger),
                    HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts,
                    HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout,
                    clusterConf.Port,
                    logger);
            });
    }

    #endregion

    #region Instances
    
    private readonly ConcurrentDictionary<SchedulerEndpoint, IConnectionPool> _schedulerConnectionPoolSingletons = new();
    
    private static readonly Dictionary<SchedulerType, SchedulerFactory> _schedulerFactoryPoolSingletons = new();

    private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings =
        HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;

    public ISchedulerDataConvertor GetDataConvertor(ILogger logger)
    {
        return CreateDataConvertor(logger);
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    ///     Create scheduler
    /// </summary>
    public abstract IRexScheduler CreateScheduler(
        Cluster configuration, 
        Project project, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        long? adaptorUserId,
        IExpirioService expirio,
        string token,
        ILogger logger);

    /// <summary>
    ///     Create scheduler adapter
    /// </summary>
    protected abstract ISchedulerAdapter CreateSchedulerAdapter(ILogger logger);

    /// <summary>
    ///     Create data convertor
    /// </summary>
    protected abstract ISchedulerDataConvertor CreateDataConvertor(ILogger logger);

    /// <summary>
    ///     Create scheduler connector
    /// </summary>
    protected abstract IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, IExpirioService expirio, ILogger logger);

    #endregion
}
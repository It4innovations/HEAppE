using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using SshCaAPI;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;

/// <summary>
///     Slurm scheduler factory
/// </summary>
internal class SlurmSchedulerFactory : SchedulerFactory
{
    private static readonly object SingletonLock = new object();
    
    #region Instances

    /// <summary>
    ///     Connectors 
    /// </summary>
    private readonly ConcurrentDictionary<string, IPoolableAdapter> _connectorSingletons = new();

    /// <summary>
    ///     Scheduler singeltons
    /// </summary>
    private readonly ConcurrentDictionary<(string MasterNodeName, long projectId, DateTime? ProjectModifiedAt, long? AdaptorUserId), IRexScheduler> _schedulerSingletons = new();

    /// <summary>
    ///     Convertor
    /// </summary>
    private ISchedulerDataConvertor _convertorSingleton;

    /// <summary>
    ///     Scheduler adapter
    /// </summary>
    private ISchedulerAdapter _schedulerAdapterInstance;

    #endregion

    #region SchedulerFactory Members

    /// <summary>
    ///     Create scheduler
    /// </summary>
    public override IRexScheduler CreateScheduler(Cluster configuration, Project project, ISshCertificateAuthorityService sshCertificateAuthorityService, long? adaptorUserId)
    {
        var uniqueIdentifier = (configuration.MasterNodeName, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);
        
        return _schedulerSingletons.GetOrAdd(
            uniqueIdentifier, 
            key => new RexSchedulerWrapper
            (
                GetSchedulerConnectionPool(configuration, project, sshCertificateAuthorityService, adaptorUserId: adaptorUserId),
                CreateSchedulerAdapter()
            )
        );
    }

    /// <summary>
    ///     Create scheduler adapter
    /// </summary>
    protected override ISchedulerAdapter CreateSchedulerAdapter()
    {
        if (_schedulerAdapterInstance == null)
        {
            lock (SingletonLock)
            {
                if (_schedulerAdapterInstance == null)
                {
                    _schedulerAdapterInstance = new SlurmSchedulerAdapter(CreateDataConvertor());
                }
            }
        }
        return _schedulerAdapterInstance;
    }

    /// <summary>
    ///     Create data convertor
    /// </summary>
    protected override ISchedulerDataConvertor CreateDataConvertor()
    {
        if (_convertorSingleton == null)
        {
            lock (SingletonLock)
            {
                if (_convertorSingleton == null)
                {
                    _convertorSingleton = new SlurmDataConvertor(new SlurmConversionAdapterFactory());
                }
            }
        }
        return _convertorSingleton;
    }

    /// <summary>
    ///     Create scheduler connector
    /// </summary>
    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        var masterNodeName = configuration.MasterNodeName;
        
        return _connectorSingletons.GetOrAdd(
            masterNodeName, 
            key => new SshConnector(sshCertificateAuthorityService)
        );
    }

    #endregion
}
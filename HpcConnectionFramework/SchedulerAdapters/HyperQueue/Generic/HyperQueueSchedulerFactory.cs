using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using SshCaAPI;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic;

/// <summary>
///     HyperQueue scheduler factory
/// </summary>
internal class HyperQueueSchedulerFactory : SchedulerFactory
{
    #region Instances

    /// <summary>
    ///     Connectors
    /// </summary>
    private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new();

    /// <summary>
    ///     Scheduler singeltons
    /// </summary>
    private readonly Dictionary<(string, long projectId, DateTime?, long?), IRexScheduler> _schedulerSingletons = new();

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

    public override IRexScheduler CreateScheduler(Cluster configuration, Project project, ISshCertificateAuthorityService sshCertificateAuthorityService, long? adaptorUserId)
    {
        var uniqueIdentifier = (configuration.MasterNodeName, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);
        if (!_schedulerSingletons.ContainsKey(uniqueIdentifier))
            _schedulerSingletons[uniqueIdentifier] = new RexSchedulerWrapper
            (
                GetSchedulerConnectionPool(configuration, project, sshCertificateAuthorityService, adaptorUserId: adaptorUserId),
                CreateSchedulerAdapter()
            );
        return _schedulerSingletons[uniqueIdentifier];
    }

    protected override ISchedulerAdapter CreateSchedulerAdapter()
    {
        return _schedulerAdapterInstance ??= new HyperQueueSchedulerAdapter(CreateDataConvertor());
    }

    protected override ISchedulerDataConvertor CreateDataConvertor()
    {
        return _convertorSingleton ??= new HyperQueueDataConvertor(new HyperQueueConversionAdapterFactory());
    }

    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        var masterNodeName = configuration.MasterNodeName;
        if (!_connectorSingletons.ContainsKey(masterNodeName))
            _connectorSingletons[masterNodeName] = new SshConnector(sshCertificateAuthorityService);

        return _connectorSingletons[masterNodeName];
    }

    #endregion
}
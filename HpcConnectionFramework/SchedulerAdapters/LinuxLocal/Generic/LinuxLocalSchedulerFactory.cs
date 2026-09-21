using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.Services.Expirio;
using SshCaAPI;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;

/// <summary>
///     Local Linux Scheduler Factory
/// </summary>
public class LinuxLocalSchedulerFactory : SchedulerFactory
{
    #region Instances

    /// <summary>
    ///     Connector singletons
    /// </summary>
    private readonly Dictionary<string, IPoolableAdapter> _linuxConnectorSingletons = new();

    /// <summary>
    ///     Scheduler singletons
    /// </summary>
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string MasterNodeName, long projectId, long? AdaptorUserId), IRexScheduler> _linuxSchedulerSingletons = new();

    /// <summary>
    ///     Convertor singletons
    /// </summary>
    private ISchedulerDataConvertor _convertorSingleton;

    /// <summary>
    ///     Scheduler adapter singletons
    /// </summary>
    private ISchedulerAdapter _linuxSchedulerAdapterInstance;

    #endregion

    #region SchedulerFactory Members

    /// <summary>
    ///     Create scheduler
    /// </summary>
    /// <param name="configuration">Cluster</param>
    /// <param name="jobInfoProject"></param>
    /// <returns></returns>
    public override IRexScheduler CreateScheduler(
        Cluster configuration, 
        Project project, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        long? adaptorUserId,
        IExpirioService expirio,
        string token,
        ILogger logger)
    {
        var uniqueIdentifier = (configuration.MasterNodeName, project.Id, project.IsOneToOneMapping ? adaptorUserId : null);
        return _linuxSchedulerSingletons.GetOrAdd(
            uniqueIdentifier,
            key => new RexSchedulerWrapper
            (
                GetSchedulerConnectionPool(configuration, project, sshCertificateAuthorityService, adaptorUserId: adaptorUserId, expirio, logger),
                CreateSchedulerAdapter(logger), logger
            )
        );
    }

    public override void InvalidateSchedulersForProject(long projectId)
    {
        foreach (var key in _linuxSchedulerSingletons.Keys)
        {
            if (key.projectId == projectId)
            {
                _linuxSchedulerSingletons.TryRemove(key, out _);
            }
        }
    }

    public override void InvalidateSchedulersForCluster(string masterNodeName)
    {
        foreach (var key in _linuxSchedulerSingletons.Keys)
        {
            if (string.Equals(key.MasterNodeName, masterNodeName, StringComparison.OrdinalIgnoreCase))
            {
                _linuxSchedulerSingletons.TryRemove(key, out _);
            }
        }
    }

    public override void InvalidateAllSchedulers()
    {
        _linuxSchedulerSingletons.Clear();
    }

    /// <summary>
    ///     Create scheduler adapter
    /// </summary>
    /// <returns></returns>
    protected override ISchedulerAdapter CreateSchedulerAdapter(ILogger logger)
    {
        return _linuxSchedulerAdapterInstance ??= new LinuxLocalSchedulerAdapter(CreateDataConvertor(logger), logger);
    }

    /// <summary>
    ///     Create data convertor
    /// </summary>
    /// <returns></returns>
    protected override ISchedulerDataConvertor CreateDataConvertor(ILogger logger)
    {
        return _convertorSingleton ??= new LinuxLocalDataConvertor(logger);
    }

    /// <summary>
    ///     Create scheduler connector
    /// </summary>
    /// <param name="configuration">Cluster</param>
    /// <returns></returns>
    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, IExpirioService expirio, ILogger logger)
    {
        if (!_linuxConnectorSingletons.ContainsKey(configuration.MasterNodeName))
            _linuxConnectorSingletons[configuration.MasterNodeName] = new SshConnector(sshCertificateAuthorityService, expirio, logger);
        return _linuxConnectorSingletons[configuration.MasterNodeName];
    }

    #endregion
}
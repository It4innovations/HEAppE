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
    private readonly Dictionary<(string, long projectId, DateTime?, long?), IRexScheduler> _linuxSchedulerSingletons = new();

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
        var uniqueIdentifier = (configuration.MasterNodeName, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);
        if (!_linuxSchedulerSingletons.ContainsKey(uniqueIdentifier))
            _linuxSchedulerSingletons[uniqueIdentifier] = new RexSchedulerWrapper
            (
                GetSchedulerConnectionPool(configuration, project, sshCertificateAuthorityService, adaptorUserId: adaptorUserId, expirio, logger),
                CreateSchedulerAdapter(logger), logger
            );
        return _linuxSchedulerSingletons[uniqueIdentifier];
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
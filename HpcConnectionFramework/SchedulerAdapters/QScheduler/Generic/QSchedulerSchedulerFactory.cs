using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.Services.Expirio;
using HEAppE.FileTransferFramework;
using SshCaAPI;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;

/// <summary>
///     QScheduler scheduler factory
/// </summary>
internal class QSchedulerSchedulerFactory : SchedulerFactory
{
    #region Instances

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    ///     Connectors
    /// </summary>
    private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new();

    /// <summary>
    ///     Scheduler singletons
    /// </summary>
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string MasterNodeName, long projectId, long? AdaptorUserId), IRexScheduler> _schedulerSingletons = new();

    /// <summary>
    ///     Convertor
    /// </summary>
    private ISchedulerDataConvertor _convertorSingleton;

    /// <summary>
    ///     Scheduler adapter
    /// </summary>
    private ISchedulerAdapter _schedulerAdapterInstance;

    #endregion

    public QSchedulerSchedulerFactory()
    {
        using var serviceScope = ServiceActivator.GetScope();
        _httpClientFactory = (IHttpClientFactory)serviceScope.ServiceProvider.GetService(typeof(IHttpClientFactory));
    }

    #region SchedulerFactory Members

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
        return _schedulerSingletons.GetOrAdd(
            uniqueIdentifier,
            key =>
            {
                var wrapper = new RexSchedulerWrapper
                (
                    GetSchedulerConnectionPool(configuration, project, sshCertificateAuthorityService, adaptorUserId: adaptorUserId, expirio, logger),
                    CreateSchedulerAdapter(logger), logger
                );
                wrapper.Project = new Project
                {
                    Id = project.Id,
                    Name = project.Name,
                    AccountingString = project.AccountingString,
                    UsageType = project.UsageType,
                    ClusterProjects = project.ClusterProjects,
                    ProjectClusterNodeTypeAggregations = project.ProjectClusterNodeTypeAggregations != null
                        ? System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(project.ProjectClusterNodeTypeAggregations, a => new ProjectClusterNodeTypeAggregation
                        {
                            AllocationAmount = a.AllocationAmount,
                            ClusterNodeTypeAggregation = a.ClusterNodeTypeAggregation != null ? new ClusterNodeTypeAggregation() : null
                        }))
                        : null
                };
                return wrapper;
            }
        );
    }

    public override void InvalidateSchedulersForProject(long projectId)
    {
        foreach (var key in _schedulerSingletons.Keys)
        {
            if (key.projectId == projectId)
            {
                _schedulerSingletons.TryRemove(key, out _);
            }
        }
    }

    public override void InvalidateSchedulersForCluster(string masterNodeName)
    {
        foreach (var key in _schedulerSingletons.Keys)
        {
            if (string.Equals(key.MasterNodeName, masterNodeName, StringComparison.OrdinalIgnoreCase))
            {
                _schedulerSingletons.TryRemove(key, out _);
            }
        }
    }

    public override void InvalidateAllSchedulers()
    {
        _schedulerSingletons.Clear();
    }

    protected override ISchedulerAdapter CreateSchedulerAdapter(ILogger logger)
    {
        return _schedulerAdapterInstance ??= new QSchedulerSchedulerAdapter(CreateDataConvertor(logger), _httpClientFactory, logger);
    }

    protected override ISchedulerDataConvertor CreateDataConvertor(ILogger logger)
    {
        return _convertorSingleton ??= new QSchedulerDataConvertor(logger);
    }

    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, IExpirioService expirio, ILogger logger)
    {
        var masterNodeName = configuration.MasterNodeName;
        if (!_connectorSingletons.ContainsKey(masterNodeName))
        {
            if (configuration.ConnectionProtocol == ClusterConnectionProtocol.Http || configuration.ConnectionProtocol == ClusterConnectionProtocol.Https)
            {
                _connectorSingletons[masterNodeName] = new HttpConnector();
            }
            else
            {
                _connectorSingletons[masterNodeName] = new SshConnector(sshCertificateAuthorityService, expirio, logger);
            }
        }

        return _connectorSingletons[masterNodeName];
    }

    #endregion
}

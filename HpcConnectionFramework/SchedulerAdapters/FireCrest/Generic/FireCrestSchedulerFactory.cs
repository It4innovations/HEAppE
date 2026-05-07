using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using System;
using System.Collections.Generic;
using System.Xml.Schema;


namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

/// <summary>
///     FirecRest scheduler factory for creating and managing FirecRest-related components
/// </summary>
internal class FirecRestSchedulerFactory : SchedulerFactory
{
    #region Instances

    /// <summary>
    ///     Connector instances mapped by cluster master node name
    /// </summary>
    private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new();

    /// <summary>
    ///     Scheduler instances mapped by cluster and project details
    /// </summary>
    private readonly Dictionary<(string, long projectId, DateTime?, long?), IRexScheduler> _schedulerSingletons = new();

    /// <summary>
    ///     Scheduler instances mapped by cluster and project details
    /// </summary>
    private readonly Dictionary<(string, long projectId, DateTime?, long?), FirecRestSchedulerAdapter> _schedulerAdapters = new();

    /// <summary>
    ///     Data convertor singleton for translating between HEAppE and FirecRest formats
    /// </summary>
    private ISchedulerDataConvertor _convertorSingleton;

    /// <summary>
    ///     Scheduler adapter instance for interacting with FirecRest API
    /// </summary>
    private ISchedulerAdapter _schedulerAdapterInstance;

    #endregion

    #region SchedulerFactory Members

    /// <summary>
    ///     Create or get existing scheduler instance for the specified cluster and project
    /// </summary>
    /// <param name="configuration">Cluster configuration data</param>
    /// <param name="project">Project information</param>
    /// <param name="adaptorUserId">Optional adapter user ID for one-to-one mapping</param>
    /// <returns>Scheduler instance</returns>
    public override IRexScheduler CreateScheduler(
        Cluster configuration,
        Project project,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        long? adaptorUserId,
        IExpirioService expirio,
        ILogger logger,
        Dictionary<string, dynamic> options = null)
    {
        // use masterNodeName to have unique scheduler for various Expirio users
        var masterNodeName = configuration.MasterNodeName;
        if (options != null)
        {
            if (options.TryGetValue("f7t_url", out dynamic value))
                masterNodeName += "|" + value;
            if (options.TryGetValue("f7t_token_url", out value))
                masterNodeName += "|" + value;
            if (options.TryGetValue("f7t_client_id", out value))
                masterNodeName += "|" + value;
        }

        var uniqueIdentifier = (masterNodeName, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);

        FirecRestSchedulerAdapter schedulerAdapter = null;
        if (!_schedulerSingletons.ContainsKey(uniqueIdentifier))
        {
            schedulerAdapter = CreateSchedulerAdapter(logger) as FirecRestSchedulerAdapter;
            _schedulerSingletons[uniqueIdentifier] = new RexSchedulerWrapper
            (
                null, // ssh connection pool not needed for FirecREST
                schedulerAdapter,
                logger
            );
            _schedulerAdapters[uniqueIdentifier] = schedulerAdapter;
        }

        if (configuration.FirecRestEndpointId != null)
        {
            schedulerAdapter.FirecRestUrl = configuration.FirecRestEndpoint.Url;
            schedulerAdapter.TokenEndpoint = configuration.FirecRestEndpoint.IdpUrl;
        }

        // set or update values
        schedulerAdapter ??= _schedulerAdapters[uniqueIdentifier];
        if (options != null)
        {
            if (options.TryGetValue("f7t_url", out dynamic value))
                schedulerAdapter.FirecRestUrl = value;
            if (options.TryGetValue("f7t_token_url", out value))
                schedulerAdapter.TokenEndpoint = value;
            if (options.TryGetValue("f7t_client_id", out value))
                schedulerAdapter.ClientId = value;
            if (options.TryGetValue("f7t_client_secret", out value))
                schedulerAdapter.ClientSecret = value;
        }

        return _schedulerSingletons[uniqueIdentifier];
    }

    /// <summary>
    ///     Create or get existing FirecRest scheduler adapter instance
    /// </summary>
    /// <returns>FirecRest scheduler adapter</returns>
    protected override ISchedulerAdapter CreateSchedulerAdapter(ILogger logger)
    {
        return _schedulerAdapterInstance ??= new FirecRestSchedulerAdapter(CreateDataConvertor(logger), logger);
    }

    /// <summary>
    ///     Create or get existing FirecRest data convertor
    /// </summary>
    /// <returns>FirecRest data convertor</returns>
    protected override ISchedulerDataConvertor CreateDataConvertor(ILogger logger)
    {
        return _convertorSingleton ??= new FirecRestDataConvertor(new FirecRestConversionAdapterFactory(), logger);
    }

    /// <summary>
    ///     Create or get existing SSH connector for the specified cluster
    /// </summary>
    /// <param name="configuration">Cluster configuration data</param>
    /// <returns>SSH connector instance</returns>
    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, IExpirioService expirio, ILogger logger)
    {
        throw new NotImplementedException("CreateSchedulerConnector not implemented in FirecRestSchedulerFactory");
    }

    #endregion
}

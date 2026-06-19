using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using HEAppE.Exceptions.Internal;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SshCaAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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

    /// <summary>
    ///     Scheduler type
    /// </summary>
    private SchedulerType _schedulerType;
    private readonly IHttpClientFactory _httpClientFactory;

    #endregion

    #region SchedulerFactory Members

    public FirecRestSchedulerFactory(SchedulerType schedulerType)
    {
        using var serviceScope = ServiceActivator.GetScope();
        _schedulerType = schedulerType;
        _httpClientFactory = (IHttpClientFactory)serviceScope.ServiceProvider.GetService(typeof(IHttpClientFactory));
    }

    /// <summary>
    ///     Create or get existing scheduler instance for the specified cluster and project
    /// </summary>
    /// <param name="cluster">Cluster configuration data</param>
    /// <param name="project">Project information</param>
    /// <param name="adaptorUserId">Optional adapter user ID for one-to-one mapping</param>
    /// <returns>Scheduler instance</returns>
    public override IRexScheduler CreateScheduler(
        Cluster cluster,
        Project project,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        long? adaptorUserId,
        IExpirioService expirio,
        string token,
        ILogger logger)
    {
        string protocol = cluster.ConnectionProtocol == ClusterConnectionProtocol.Http ? "http" : "https";
        string url = $"{protocol}://{cluster.MasterNodeName}";
        string idpUrl = "";

        if (cluster.CustomConfiguration != null && cluster.CustomConfiguration.TryGetValue("IdpUrl", out var customIdpUrl))
        {
            idpUrl = customIdpUrl;
        }

        string clientId = "";
        string clientSecret = "";

        dynamic value;
        Dictionary<string, dynamic> options = Task.Run(async () => await GetSchedulerOptions(cluster, token, expirio, logger)).Result;

        // try get values provided by Expirio
        if (options != null)
        {
            // get clientId and clientSecret for use with FirecRest's keycloak
            if (options.TryGetValue("f7t_client_id", out value))
                clientId = value;
            if (options.TryGetValue("f7t_client_secret", out value))
                clientSecret = value;

            // it is still possible to override url and idpUrl if enabled by metadata
            if (options.TryGetValue("f7t_url", out value))
                url = value;
            if (options.TryGetValue("f7t_token_url", out value))
                idpUrl = value;
        }

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new FirecRestException("FirecRest credentials must be obtained from Expirio. Direct access via Proxy Connection is not allowed.")
            {
                CommandError = "Missing Expirio credentials"
            };
        }

        // use masterNodeName to have unique scheduler for various Expirio users
        var uniqueKey = $"_FirecRest|{url}|{idpUrl}";
        if (!string.IsNullOrEmpty(clientId))
            uniqueKey += $"|{clientId}";
        if (!string.IsNullOrEmpty(clientSecret))
            uniqueKey += $"|{clientSecret}";

        // try to get existing scheduler
        var uniqueIdentifier = (uniqueKey, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);

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

        // set or update values
        schedulerAdapter ??= _schedulerAdapters[uniqueIdentifier];
        
        schedulerAdapter.FirecRestUrl = url;
        schedulerAdapter.FirecRestIdpUrl = idpUrl;
        schedulerAdapter.ClientId = clientId;
        schedulerAdapter.ClientSecret = clientSecret;

        return _schedulerSingletons[uniqueIdentifier];
    }

    /// <summary>
    ///     Create or get existing FirecRest scheduler adapter instance
    /// </summary>
    /// <returns>FirecRest scheduler adapter</returns>
    protected override ISchedulerAdapter CreateSchedulerAdapter(ILogger logger)
    {
        return _schedulerAdapterInstance ??= new FirecRestSchedulerAdapter(CreateDataConvertor(logger), _httpClientFactory, logger);
    }

    /// <summary>
    ///     Create or get existing FirecRest data convertor
    /// </summary>
    /// <returns>FirecRest data convertor</returns>
    protected override ISchedulerDataConvertor CreateDataConvertor(ILogger logger)
    {
        ConversionAdapterFactory conversionAdapterFactory = null;
        if (_schedulerType.HasFlag(SchedulerType.PbsPro))
            conversionAdapterFactory = new PbsProConversionAdapterFactory();
        else if (_schedulerType.HasFlag(SchedulerType.Slurm) || _schedulerType == SchedulerType.FirecRESTSlurm)
            conversionAdapterFactory = new SlurmConversionAdapterFactory();

        return _convertorSingleton ??= new FirecRestDataConvertor(conversionAdapterFactory, logger);
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

    protected async Task<Dictionary<string, dynamic>> GetSchedulerOptions(Cluster cluster, string token, IExpirioService expirioService, ILogger logger)
    {
        Dictionary<string, dynamic> result = [];
        if (cluster.SchedulerType.HasFlag(SchedulerType.FirecRESTSlurm))
        {
            if (!String.IsNullOrEmpty(token) && cluster.CustomConfiguration != null)
            {
                result = result.Concat(await expirioService.ExchangeFirecrestCredentialsAsync(token, cluster.CustomConfiguration, logger)).ToDictionary();
            }
        }
        return result;
    }

    #endregion
}

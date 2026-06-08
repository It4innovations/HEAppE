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
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SshCaAPI;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private readonly IOptionsMonitor<FirecRestConfiguration> _firecRestConfiguration;

    #endregion

    #region SchedulerFactory Members

    public FirecRestSchedulerFactory(SchedulerType schedulerType)
    {
        using var serviceScope = ServiceActivator.GetScope();

        _schedulerType = schedulerType;
        _firecRestConfiguration = (IOptionsMonitor<FirecRestConfiguration>)serviceScope.ServiceProvider.GetService(typeof(IOptionsMonitor<FirecRestConfiguration>));
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
        string url, idpUrl, clientId = "", clientSecret = "";
        dynamic value;

        Dictionary<string, dynamic> options = Task.Run(async () => await GetSchedulerOptions(cluster, token, expirio, logger)).Result;

        if (cluster.ProxyConnection?.FirecRestOptions != null)
        {
            // take values from FirecRest options in proxy
            url = cluster.ProxyConnection.FirecRestOptions.Url;
            idpUrl = cluster.ProxyConnection.FirecRestOptions.IdpUrl;

            // proxy username and password => default credentials for FirecREST
            clientId = cluster.ProxyConnection.Username;
            clientSecret = cluster.ProxyConnection.Password;
        }
        else
        {
            // fallback to configuration in appsettings.json
            FirecRestOptions firecRestOptions = null;
            if (options != null && options.TryGetValue("FirecRestOptions", out value))
                firecRestOptions = value;

            if (firecRestOptions == null)
                throw new Exception("No options for FirecREST found!");
            
            // setup FirecREST urls
            url = firecRestOptions.Url;
            idpUrl = firecRestOptions.IdpUrl;
        }

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
        return _schedulerAdapterInstance ??= new FirecRestSchedulerAdapter(CreateDataConvertor(logger), logger);
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
        else if (_schedulerType.HasFlag(SchedulerType.Slurm))
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
        if (cluster.SchedulerType.HasFlag(SchedulerType.FirecRest))
        {
            // get firecrest options
            FirecRestOptions firecRestOptions = (cluster?.ProxyConnection?.FirecRestOptions) ?? _firecRestConfiguration.CurrentValue.FirecRestOptions[cluster.MasterNodeName];
            if (firecRestOptions != null)
                result.Add("FirecRestOptions", firecRestOptions);

            // get expirio credentials
            //var token = !string.IsNullOrEmpty(httpContextKeys.Context.FIPToken) ? httpContextKeys.Context.FIPToken : httpContextKeys.Context.LEXISToken;
            if (!String.IsNullOrEmpty(token) && firecRestOptions != null)
            {
                result = result.Concat(await expirioService.ExchangeFirecrestCredentialsAsync(token, firecRestOptions, logger)).ToDictionary();
            }
        }
        return result;
    }

    #endregion
}

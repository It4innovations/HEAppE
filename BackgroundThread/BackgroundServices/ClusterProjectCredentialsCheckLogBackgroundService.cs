using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class ClusterProjectCredentialsCheckLogBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IExpirioService _expirioService;
    private readonly BackGroundThreadConfiguration _configuration;

    public ClusterProjectCredentialsCheckLogBackgroundService(
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        BackGroundThreadConfiguration configuration,
        IExpirioService expirioService)
    {
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.ClusterProjectCredentialsCheckLogBackgroundService");
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _expirioService = expirioService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_configuration.ClusterProjectCredentialsCheckSettings.IsEnabled || SshCaSettings.UseCertificateAuthorityForAuthentication)
            {
                await Task.Delay(TimeSpan.FromMinutes(_configuration.ClusterProjectCredentialsCheckSettings.IntervalMinutes), stoppingToken);
                continue;
            }

            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                try
                {
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
                    IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory()
                        .CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, httpContextKeys, _expirioService, _logger);
                    
                    await managementLogic.CheckClusterProjectCredentialsStatus();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured during execution of the ClusterProjectCredentialsCheckLog background service. ");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_configuration.ClusterProjectCredentialsCheckSettings.IntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
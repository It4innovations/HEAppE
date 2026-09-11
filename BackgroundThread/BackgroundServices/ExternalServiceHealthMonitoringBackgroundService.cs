using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
/// Periodically probes all external services, persists telemetry logs, and purges stale entries.
/// </summary>
internal class ExternalServiceHealthMonitoringBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IExpirioService _expirioService;
    private readonly BackGroundThreadConfiguration _configuration;

    public ExternalServiceHealthMonitoringBackgroundService(
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        BackGroundThreadConfiguration configuration,
        IExpirioService expirioService)
    {
        _logger = loggerFactory.CreateLogger(
            "HEAppE.BackgroundThread.BackgroundServices.ExternalServiceHealthMonitoringBackgroundService");
        _sshCertificateAuthorityService = sshCertificateAuthorityService
            ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _expirioService = expirioService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _configuration.ExternalServiceHealthMonitoringSettings;

            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
                continue;
            }

            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                try
                {
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
                    IHttpContextKeys httpContextKeys =
                        scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory()
                        .CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, httpContextKeys,
                            _expirioService, _logger);

                    // Perform all service probes
                    var report = await managementLogic.GetExternalServicesReport(null, null);

                    // Persist each probe result as a telemetry log
                    foreach (var status in report.LiveStatus)
                    {
                        var log = new ExternalServiceHealthLog
                        {
                            ServiceName = status.ServiceName,
                            ServiceType = status.Type,
                            Protocol = status.Protocol,
                            EndpointOrHost = status.EndpointOrHost,
                            Port = status.Port,
                            CommandOrPath = null,
                            IsAvailable = status.IsAvailable,
                            ResponseTimeMs = status.ResponseTimeMs,
                            ErrorMessage = status.ErrorMessage,
                            Timestamp = status.LastCheck
                        };

                        await managementLogic.LogExternalServiceHealth(log);
                    }

                    // Purge old logs outside retention window
                    await managementLogic.PurgeOldExternalServiceHealthLogs();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "An error occurred during execution of the ExternalServiceHealthMonitoring background service.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }
    }
}

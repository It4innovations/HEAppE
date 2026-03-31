using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.ExternalAuthentication.Configuration;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class ClusterProjectCredentialsCheckLogBackgroundService : BackgroundService
{
    private readonly ILog _log;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly BackGroundThreadConfiguration _configuration;

    public ClusterProjectCredentialsCheckLogBackgroundService(
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory,
        BackGroundThreadConfiguration configuration)
    {
        _log = LogManager.GetLogger(GetType());
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
        _configuration = configuration;
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
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                    IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory()
                        .CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, httpContextKeys);
                    
                    await managementLogic.CheckClusterProjectCredentialsStatus();
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the ClusterProjectCredentialsCheckLog background service: ", ex);
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
using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class UpdateUnfinishedJobsBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;
    private readonly BackGroundThreadConfiguration _configuration;

    public UpdateUnfinishedJobsBackgroundService(
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        BackGroundThreadConfiguration configuration,
        IExpirioService expirioService)
    {
        _userOrgService = userOrgService;
        _expirioService = expirioService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.UpdateUnfinishedJobsBackgroundService");
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (SshCaSettings.UseCertificateAuthorityForAuthentication) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                try
                {
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
                    IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();
                    IExpirioService expirioService = scope.ServiceProvider.GetRequiredService<IExpirioService>();

                    await LogicFactory.GetLogicFactory()
                        .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys, expirioService, _logger)
                        .UpdateCurrentStateOfUnfinishedJobs();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured during execution of the UpdateUnfinishedJobs background service.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_configuration.GetAllJobsInformationCheck), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
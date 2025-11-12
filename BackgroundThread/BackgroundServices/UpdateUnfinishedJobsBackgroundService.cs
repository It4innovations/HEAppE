using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Get all unfinished jobs from db and load their status from cluster
///     and updates their status in DB
/// </summary>
internal class UpdateUnfinishedJobsBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.GetAllJobsInformationCheck);
    protected readonly ILog _log;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    protected readonly IHttpContextKeys _httpContextKeys;

    public UpdateUnfinishedJobsBackgroundService(ISshCertificateAuthorityService sshCertificateAuthorityService, IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IHttpContextKeys>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                    LogicFactory.GetLogicFactory()
                        .CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys)
                        .UpdateCurrentStateOfUnfinishedJobs();
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the UpdateUnfinishedJobs background service: ",
                        ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}

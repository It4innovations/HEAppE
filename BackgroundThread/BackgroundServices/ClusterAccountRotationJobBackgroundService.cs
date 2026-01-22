using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Job for managin cluster account rotation
/// </summary>
internal class ClusterAccountRotationJobBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.ClusterAccountRotationJobCheck);
    protected readonly ILog _log;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    protected readonly IHttpContextKeys _httpContextKeys;
    protected readonly IUserOrgService _userOrgService;

    public ClusterAccountRotationJobBackgroundService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IHttpContextKeys>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (!JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
                    {
                        using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();

                        //Get all jobs in state - waiting for user 
                        var allWaitingJobs = unitOfWork.SubmittedJobInfoRepository.GetAllWaitingForServiceAccount();

                        //Try to submit them again
                        foreach (var job in allWaitingJobs)
                        {
                            _log.Info($"Trying to submit waiting job {job.Id} for user {job.Submitter}");
                            LogicFactory.GetLogicFactory()
                                .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
                                .SubmitJob(job.Id, job.Submitter);
                        }

                    }
                }
                catch (Exception ex)
                {
                    _log.Error(
                        "An error occured during execution of the ClusterAccountRotationJob background service: ", ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}

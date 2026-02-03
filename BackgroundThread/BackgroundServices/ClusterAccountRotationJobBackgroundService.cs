using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class ClusterAccountRotationJobBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.ClusterAccountRotationJobCheck);
    private readonly ILog _log;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IUserOrgService _userOrgService;

    public ClusterAccountRotationJobBackgroundService(
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (JwtTokenIntrospectionConfiguration.IsEnabled) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                try
                {
                    if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
                    {
                        using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                        IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                        var allWaitingJobs = unitOfWork.SubmittedJobInfoRepository.GetAllWaitingForServiceAccount();

                        foreach (var job in allWaitingJobs)
                        {
                            try
                            {
                                _log.Info($"Trying to submit waiting job {job.Id} for user {job.Submitter}");
                                LogicFactory.GetLogicFactory()
                                    .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys)
                                    .SubmitJob(job.Id, job.Submitter);
                            }
                            catch (Exception jobEx)
                            {
                                _log.Error($"Failed to resubmit job {job.Id}: ", jobEx);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the ClusterAccountRotationJob background service: ", ex);
                }
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
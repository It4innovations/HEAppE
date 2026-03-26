using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
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

internal class ClusterAccountRotationJobBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;
    private readonly BackGroundThreadConfiguration _configuration;

    public ClusterAccountRotationJobBackgroundService(
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory, 
        ILoggerFactory loggerFactory,
        BackGroundThreadConfiguration configuration,
        IExpirioService expirioService)
    {
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.ClusterAccountRotationJobBackgroundService");
        _userOrgService = userOrgService;
        _expirioService = expirioService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _scopeFactory = scopeFactory;
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
                    if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
                    {
                        using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
                        IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                        var allWaitingJobs = unitOfWork.SubmittedJobInfoRepository.GetAllWaitingForServiceAccount();

                        foreach (var job in allWaitingJobs)
                        {
                            try
                            {
                                HEAppE.Utils.LoggingUtils.AddJobIdToLogThreadContext(job.Id);
                                if (job.Submitter != null)
                                {
                                    HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(
                                        job.Submitter.Id, job.Submitter.Username, job.Submitter.Email);
                                }

                                _logger.LogInformation($"Trying to submit waiting job {job.Id} for user {job.Submitter}");
                                LogicFactory.GetLogicFactory()
                                    .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys, _expirioService, _logger)
                                    .SubmitJob(job.Id, job.Submitter);
                            }
                            catch (Exception jobEx)
                            {
                                _logger.LogError($"Failed to resubmit job {job.Id}: ", jobEx);
                            }
                            finally
                            {
                                HEAppE.Utils.LoggingUtils.RemoveJobIdFromLogThreadContext();
                                if (job.Submitter != null)
                                {
                                    HEAppE.Utils.LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured during execution of the ClusterAccountRotationJob background service: ");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_configuration.ClusterAccountRotationJobCheck), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
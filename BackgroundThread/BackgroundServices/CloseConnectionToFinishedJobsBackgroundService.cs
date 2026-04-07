using System;
using System.Linq;
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

internal class CloseConnectionToFinishedJobsBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IUserOrgService _userOrgService;
    private readonly IExpirioService _expirioService;
    private readonly BackGroundThreadConfiguration _configuration;

    public CloseConnectionToFinishedJobsBackgroundService(
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory, 
        ILoggerFactory loggerFactory,
        BackGroundThreadConfiguration configuration,
        IExpirioService expirioService)
    {
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.CloseConnectionToFinishedJobsBackgroundService");
        _userOrgService = userOrgService;
        _expirioService = expirioService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
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
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
                    IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                    var dataTransferLogic = LogicFactory.GetLogicFactory()
                        .CreateDataTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys, _expirioService, _logger);

                    var jobManagementLogic = LogicFactory.GetLogicFactory()
                        .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys, _expirioService, _logger);

                    var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
                    
                    var finishedTasks = jobManagementLogic.GetAllFinishedTaskInfos(taskIds).ToList();
                    
                    foreach (var task in finishedTasks)
                    {
                        try
                        {
                            HEAppE.Utils.LoggingUtils.AddJobIdToLogThreadContext(task.Specification.JobSpecification.Id);
                            if (task.Specification.JobSpecification.Submitter != null)
                            {
                                HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(
                                    task.Specification.JobSpecification.Submitter.Id, 
                                    task.Specification.JobSpecification.Submitter.Username, 
                                    task.Specification.JobSpecification.Submitter.Email);
                            }
                            await dataTransferLogic.CloseAllTunnelsForTask(task);
                        }
                        catch (Exception closeEx)
                        {
                            _logger.LogWarning($"Failed to close tunnels for task {task.Id}: ", closeEx);
                        }
                        finally
                        {
                            HEAppE.Utils.LoggingUtils.RemoveJobIdFromLogThreadContext();
                            if (task.Specification.JobSpecification.Submitter != null)
                            {
                                HEAppE.Utils.LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured during execution of the CloseConnectionToFinishedJobs background service: ");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_configuration.CloseConnectionToFinishedJobsCheck), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

internal class CloseConnectionToFinishedJobsBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.CloseConnectionToFinishedJobsCheck);
    private readonly ILog _log;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IUserOrgService _userOrgService;

    public CloseConnectionToFinishedJobsBackgroundService(
        IUserOrgService userOrgService, 
        ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
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
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                    IHttpContextKeys httpContextKeys = scope.ServiceProvider.GetRequiredService<IHttpContextKeys>();

                    var dataTransferLogic = LogicFactory.GetLogicFactory()
                        .CreateDataTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys);

                    var jobManagementLogic = LogicFactory.GetLogicFactory()
                        .CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, httpContextKeys);

                    var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
                    
                    var finishedTasks = jobManagementLogic.GetAllFinishedTaskInfos(taskIds).ToList();
                    
                    foreach (var task in finishedTasks)
                    {
                        try
                        {
                            dataTransferLogic.CloseAllTunnelsForTask(task);
                        }
                        catch (Exception closeEx)
                        {
                            _log.Warn($"Failed to close tunnels for task {task.Id}: ", closeEx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the CloseConnectionToFinishedJobs background service: ", ex);
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
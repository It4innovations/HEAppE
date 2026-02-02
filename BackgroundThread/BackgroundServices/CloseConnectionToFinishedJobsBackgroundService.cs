using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Close all open sockets to finished/failed/canceled jobs
/// </summary>
internal class CloseConnectionToFinishedJobsBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.CloseConnectionToFinishedJobsCheck);
    protected readonly ILog _log;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    protected readonly IHttpContextKeys _httpContextKeys;
    protected readonly IUserOrgService _userOrgService;
    public CloseConnectionToFinishedJobsBackgroundService(IUserOrgService userOrgService,ISshCertificateAuthorityService sshCertificateAuthorityService, IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
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
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                    var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);

                    var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
                    LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys).GetAllFinishedTaskInfos(taskIds)
                        .ToList()
                        .ForEach(f => dataTransferLogic.CloseAllTunnelsForTask(f));
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the CloseConnectionToFinishedJobs background service: ", ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}

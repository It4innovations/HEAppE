using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using System.Threading;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Remove temporary provided File transfer Key per jobs
/// </summary>
internal class RemoveTemporaryFileTransferKeyBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.FileTransferKeyRemovalCheck);
    protected readonly ILog _log;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    protected readonly IHttpContextKeys _httpContextKeys;
    protected readonly IUserOrgService _userOrgService;
    

    public RemoveTemporaryFileTransferKeyBackgroundService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IServiceScopeFactory scopeFactory)
    {
        _userOrgService = userOrgService;
        _log = LogManager.GetLogger(GetType());
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
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                    LogicFactory.GetLogicFactory()
                        .CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
                        .RemoveJobsTemporaryFileTransferKeys();
                }
                catch (Exception ex)
                {
                    _log.Error(
                        "An error occured during execution of the RemoveTemporaryFileTransferKey background service: ",
                        ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}

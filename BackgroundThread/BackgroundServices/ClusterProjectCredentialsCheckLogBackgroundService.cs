using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
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
///     ClusterProjectCredentialsCheckLog
/// </summary>
internal class ClusterProjectCredentialsCheckLogBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(BackGroundThreadConfiguration.ClusterProjectCredentialsCheckConfiguration.IntervalMinutes);
    protected readonly ILog _log;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    protected readonly IHttpContextKeys _httpContextKeys;

    public ClusterProjectCredentialsCheckLogBackgroundService(ISshCertificateAuthorityService sshCertificateAuthorityService, IServiceScopeFactory scopeFactory)
    {
        _log = LogManager.GetLogger(GetType());
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IHttpContextKeys>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (BackGroundThreadConfiguration.ClusterProjectCredentialsCheckConfiguration.IsEnabled && !JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (IUnitOfWork unitOfWork = new DatabaseUnitOfWork())
                    {
                        IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
                        await managementLogic.CheckClusterProjectCredentialsStatus();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the ClusterProjectCredentialsCheckLog background service: ", ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }    
        }
        
    }
}

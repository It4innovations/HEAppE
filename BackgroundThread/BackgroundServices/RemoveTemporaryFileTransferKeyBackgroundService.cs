using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using System.Threading;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
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

    public RemoveTemporaryFileTransferKeyBackgroundService(ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _log = LogManager.GetLogger(GetType());
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService).RemoveJobsTemporaryFileTransferKeys();
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the RemoveTemporaryFileTransferKey background service: ", ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;
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

    public CloseConnectionToFinishedJobsBackgroundService(ISshCertificateAuthorityService sshCertificateAuthorityService)
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
                var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _sshCertificateAuthorityService);

                var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
                LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService).GetAllFinishedTaskInfos(taskIds)
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

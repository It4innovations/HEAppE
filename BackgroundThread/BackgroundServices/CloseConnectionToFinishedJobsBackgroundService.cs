using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Close all open sockets to finished/failed/canceled jobs
/// </summary>
internal class CloseConnectionToFinishedJobsBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.CloseConnectionToFinishedJobsCheck);
    protected readonly ILog _log;

    public CloseConnectionToFinishedJobsBackgroundService()
    {
        _log = LogManager.GetLogger(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
                var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);

                var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
                LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetAllFinishedTaskInfos(taskIds)
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

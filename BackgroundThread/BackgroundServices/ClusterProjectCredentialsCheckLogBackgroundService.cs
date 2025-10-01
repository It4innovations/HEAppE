using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     ClusterProjectCredentialsCheckLog
/// </summary>
internal class ClusterProjectCredentialsCheckLogBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(BackGroundThreadConfiguration.ClusterProjectCredentialsCheckConfiguration.IntervalMinutes);
    protected readonly ILog _log;

    public ClusterProjectCredentialsCheckLogBackgroundService()
    {
        _log = LogManager.GetLogger(GetType());
    }
    private enum ExecutionStep { DoLogic, DoSave, DoCleanup }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IUnitOfWork unitOfWork = null;
        IManagementLogic managementLogic = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            for (ExecutionStep i = ExecutionStep.DoLogic; i <= ExecutionStep.DoCleanup; i++)
            {
                try
                {
                    switch (i)
                    {
                        case ExecutionStep.DoLogic:
                            unitOfWork = new DatabaseUnitOfWork();
                            managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                            await managementLogic.CheckClusterProjectCredentialsStatus();
                            break;

                        case ExecutionStep.DoSave:
                            unitOfWork?.Save();
                            break;

                        case ExecutionStep.DoCleanup:
                            managementLogic = null;
                            unitOfWork?.Dispose();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the ClusterProjectCredentialsCheckLog background service: ", ex);
                }
            }
            unitOfWork = null;

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
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
internal class ClusterProjectCredentialsCheckLog : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(BackGroundThreadConfiguration.ClusterProjectCredentialsCheckConfiguration.IntervalMinutes);
    protected readonly ILog _log;

    public ClusterProjectCredentialsCheckLog()
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
                //LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).UpdateCurrentStateOfUnfinishedJobs();

                //LogicFactory.GetLogicFactory().CreateJobManagementLogic().
                //unitOfWork.CommandTemplateRepository

                //Try to submit them again
                /*
                foreach (var job in allWaitingJobs)
                {
                    _log.Info($"Trying to submit waiting job {job.Id} for user {job.Submitter}");
                    LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).SubmitJob(job.Id, job.Submitter);
                }*/
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the ClusterProjectCredentialsCheckLog background service: ", ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

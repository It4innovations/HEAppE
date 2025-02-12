using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Configuration;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
///     Job for managin cluster account rotation
/// </summary>
internal class ClusterAccountRotationJobBackgroundService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.ClusterAccountRotationJobCheck);
    protected readonly ILog _log;

    public ClusterAccountRotationJobBackgroundService()
    {
        _log = LogManager.GetLogger(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!BusinessLogicConfiguration.SharedAccountsPoolMode)
                {
                    using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();

                    //Get all jobs in state - waiting for user 
                    var allWaitingJobs = unitOfWork.SubmittedJobInfoRepository.GetAllWaitingForServiceAccount();

                    //Try to submit them again
                    foreach (var job in allWaitingJobs)
                        LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).SubmitJob(job.Id, job.Submitter);
                }
            }
            catch (Exception ex)
            {
                _log.Error("An error occured during execution of the ClusterAccountRotationJob background service: ", ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

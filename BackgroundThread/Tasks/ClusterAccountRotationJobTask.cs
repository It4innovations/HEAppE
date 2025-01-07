using System;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.BackgroundThread.Tasks;

/// <summary>
///     Job for managin cluster account rotation
/// </summary>
internal class ClusterAccountRotationJobTask : AbstractTask, IBackgroundTask
{
    public ClusterAccountRotationJobTask(TimeSpan interval) : base(interval)
    {
    }

    protected override void RunTask()
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
}
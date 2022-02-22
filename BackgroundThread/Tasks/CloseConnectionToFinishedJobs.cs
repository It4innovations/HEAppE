using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;

namespace HEAppE.BackgroundThread.Tasks
{
    /// <summary>
    /// Close all open sockets to finished/failed/canceled jobs
    /// </summary>
    internal class CloseConnectionToFinishedJobs : AbstractTask, IBackgroundTask
    {
        public CloseConnectionToFinishedJobs(TimeSpan interval) : base(interval)
        {
        }

        protected override void RunTask()
        {
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
            var jobIds = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork).GetJobIdsForOpenTunnels();
            foreach (long jobId in jobIds)
            {
                //check the job's status
                SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(jobId);
                if (jobInfo.State is >= JobState.Finished and not JobState.WaitingForServiceAccount)
                {
                    LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork).CloseAllConnectionsForJob(jobInfo);
                }
            }
        }

    }
}
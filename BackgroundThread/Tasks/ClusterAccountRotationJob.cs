using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.BackgroundThread.Tasks
{
    /// <summary>
    /// Job for managin cluster account rotation
    /// </summary>
    internal class ClusterAccountRotationJob : AbstractTask, IBackgroundTask
    {
        public ClusterAccountRotationJob(TimeSpan interval) : base(interval) 
        {
        }

        protected override void RunTask()
        {
            if (BusinessLogicConfiguration.ClusterAccountRotation)
            {
                using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();

                //Get all jobs in state - waiting for user 
                IEnumerable<SubmittedJobInfo> allWaitingJobs = unitOfWork.SubmittedJobInfoRepository.ListAllWaitingForServiceAccount();

                //Try to submit them again
                foreach (SubmittedJobInfo job in allWaitingJobs)
                {
                    LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).SubmitJob(job.Id, job.Submitter);
                }
            } 
        }
    }
}

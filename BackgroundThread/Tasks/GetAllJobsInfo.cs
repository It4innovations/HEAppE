using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using System;

namespace HEAppE.BackgroundThread.Tasks
{
    /// <summary>
    ///   Get all unfinished jobs from db and load their status from cluster
    ///   and updates their status in DB
    /// </summary>
    internal class GetAllJobsInfo : AbstractTask, IBackgroundTask
    {
        public GetAllJobsInfo(TimeSpan interval) : base(interval)
        {
        }

        protected override void RunTask()
        {
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
            LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).UpdateCurrentStateOfUnfinishedJobs();
        }
    }
}
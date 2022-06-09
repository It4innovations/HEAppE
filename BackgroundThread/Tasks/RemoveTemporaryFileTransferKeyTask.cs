using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using System;

namespace HEAppE.BackgroundThread.Tasks
{
    /// <summary>
    /// Remove temporary provided File transfer Key per jobs
    /// </summary>
    internal class RemoveTemporaryFileTransferKeyTask : AbstractTask, IBackgroundTask
    {
        public RemoveTemporaryFileTransferKeyTask(TimeSpan interval) : base(interval)
        {
        }

        protected override void RunTask()
        {
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
            LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork).RemoveJobsTemporaryFileTransferKeys();
        }
    }
}

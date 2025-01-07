using System;
using System.Linq;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.BackgroundThread.Tasks;

/// <summary>
///     Close all open sockets to finished/failed/canceled jobs
/// </summary>
internal class CloseConnectionToFinishedJobsTask : AbstractTask, IBackgroundTask
{
    public CloseConnectionToFinishedJobsTask(TimeSpan interval) : base(interval)
    {
    }

    protected override void RunTask()
    {
        using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);

        var taskIds = dataTransferLogic.GetTaskIdsWithOpenTunnels();
        LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetAllFinishedTaskInfos(taskIds)
            .ToList()
            .ForEach(f => dataTransferLogic.CloseAllTunnelsForTask(f));
    }
}
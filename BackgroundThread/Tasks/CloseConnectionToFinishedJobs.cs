using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.ServiceTier.JobManagement;
using HEAppE.ServiceTier.DataTransfer;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;

namespace HEAppE.BackgroundThread.Tasks {
	/// <summary>
	///   Close all open sockets to finished/failed/canceled jobs
	/// </summary>
	internal class CloseConnectionToFinishedJobs : AbstractTask, IBackgroundTask {
        public CloseConnectionToFinishedJobs(TimeSpan interval) : base(interval) { }

		protected override void RunTask() {
			using (IUnitOfWork unitOfWork = new DatabaseUnitOfWork()) {
                List<long> jobIds = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork).GetJobIdsForOpenTunnels();
                foreach (long jobId in jobIds)
                {
                    //check the job's status
                    SubmittedJobInfo jobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(jobId);
                    if (jobInfo.State >= JobState.Finished && jobInfo.State != JobState.WaitingForServiceAccount)
                    {
                        LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork).CloseAllConnectionsForJob(jobInfo);
                    }
                }
			}
		}

	}
}
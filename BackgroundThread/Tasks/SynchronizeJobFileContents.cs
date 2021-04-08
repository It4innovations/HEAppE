using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;

namespace HEAppE.BackgroundThread.Tasks {
	internal class SynchronizeJobFileContents : AbstractTask {
		public SynchronizeJobFileContents(TimeSpan interval) : base(interval) {}

		protected override void RunTask() {
			using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork()) {
				IList<SynchronizedJobFiles> jobFileContents = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork).SynchronizeAllUnfinishedJobFiles();
                #warning DEPRECATED
                //new FileTransferService().SendSynchronizedJobFilesToClient(jobFileContents);
            }
        }
	}
}
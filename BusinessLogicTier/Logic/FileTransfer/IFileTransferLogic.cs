using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer {
	public interface IFileTransferLogic {
		FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser);
		void EndFileTransfer(long submittedJobInfoId, FileTransferMethod transferMethod, AdaptorUser loggedUser);
		IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser);
		IList<SynchronizedJobFiles> SynchronizeAllUnfinishedJobFiles();
		ICollection<FileInformation> ListChangedFilesForJob(long submittedJobInfoId, AdaptorUser loggedUser);
		byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser);

        FileTransferMethod GetFileTransferMethodById(long fileTransferMethodId);
		IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId);
	}
}
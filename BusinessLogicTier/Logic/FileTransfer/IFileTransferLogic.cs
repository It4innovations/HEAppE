using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer;

public interface IFileTransferLogic
{
    void RemoveJobsTemporaryFileTransferKeys();
    FileTransferMethod TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser);
    FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser);
    void EndFileTransfer(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser);

    IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets,
        AdaptorUser loggedUser);

    IList<SynchronizedJobFiles> SynchronizeAllUnfinishedJobFiles();
    ICollection<FileInformation> ListChangedFilesForJob(long submittedJobInfoId, AdaptorUser loggedUser);
    byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser);
    FileTransferMethod GetFileTransferMethodById(long fileTransferMethodId);
    IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId);
}
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

    dynamic UploadFileToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, AdaptorUser loggedUser);
    dynamic UploadJobScriptToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, AdaptorUser loggedUser);
    dynamic UploadFileToJobExecutionDir(Stream fileStream, string fileName, long createdJobInfoId, AdaptorUser loggedUser);
}
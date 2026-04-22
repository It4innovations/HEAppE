using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer;

public interface IFileTransferLogic
{
    Task RemoveJobsTemporaryFileTransferKeys();
    Task<FileTransferMethod> TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<FileTransferMethod> GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser);
    Task EndFileTransfer(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser);

    Task<IList<JobFileContent>> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets,
        AdaptorUser loggedUser);

    Task<IList<SynchronizedJobFiles>> SynchronizeAllUnfinishedJobFiles();
    Task<ICollection<FileInformation>> ListChangedFilesForJob(long submittedJobInfoId, AdaptorUser loggedUser);
    Task<byte[]> DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser);
    FileTransferMethod GetFileTransferMethodById(long fileTransferMethodId);
    IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId);

    Task<dynamic> UploadFileToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId,
        AdaptorUser loggedUser);
    Task<dynamic> UploadJobScriptToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId,
        AdaptorUser loggedUser);
    Task<dynamic> UploadFileToJobExecutionDir(Stream fileStream, string fileName, long createdJobInfoId, long? createdTaskInfoId, AdaptorUser loggedUser);
    Task<FileTransferMethod> ProvideCredentials(long modelProjectId, long modelClusterId, AdaptorUser loggedUser);
}
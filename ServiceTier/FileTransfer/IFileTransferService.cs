using HEAppE.ExtModels.FileTransfer.Models;
using System.IO;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.FileTransfer;

public interface IFileTransferService
{
    FileTransferMethodExt TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode);
    FileTransferMethodExt RequestFileTransfer(long submittedJobInfoId, string sessionCode);
    void CloseFileTransfer(long submittedJobInfoId, string publicKey, string sessionCode);

    JobFileContentExt[] DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffsetExt[] taskFileOffsets,
        string sessionCode);

    FileInformationExt[] ListChangedFilesForJob(long submittedJobInfoId, string sessionCode);
    byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, string sessionCode);

    Task<dynamic> UploadFileToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode);
    Task<dynamic> UploadJobScriptToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode);
    Task<dynamic> UploadFileToJobExecutionDir(Stream fileStream, string fileName, long createdJobInfoId, string sessionCode);
}
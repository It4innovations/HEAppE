using HEAppE.ExtModels.FileTransfer.Models;
using System.IO;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.FileTransfer;

public interface IFileTransferService
{
    Task<FileTransferMethodExt> TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode);
    Task<FileTransferMethodExt> RequestFileTransfer(long submittedJobInfoId, string sessionCode);
    Task CloseFileTransferAsync(long submittedJobInfoId, string publicKey, string sessionCode);

    Task<JobFileContentExt[]> DownloadPartsOfJobFilesFromClusterAsync(long submittedJobInfoId, TaskFileOffsetExt[] taskFileOffsets,
        string sessionCode);

    Task<FileInformationExt[]> ListChangedFilesForJobAsync(long submittedJobInfoId, string sessionCode);
    Task<byte[]> DownloadFileFromClusterAsync(long submittedJobInfoId, string relativeFilePath, string sessionCode);

    Task<dynamic> UploadFileToProjectDirAsync(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode);
    Task<dynamic> UploadJobScriptToProjectDirAsync(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode);
    Task<dynamic> UploadFileToJobExecutionDir(Stream fileStream, string fileName, long submittedJobInfoId, long? submittedTaskInfoId, string sessionCode);
    Task<FileTransferMethodExt> ProvideCredentialsAsync(long modelProjectId, long modelClusterId);
}
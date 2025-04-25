using HEAppE.ExtModels.FileTransfer.Models;

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
}
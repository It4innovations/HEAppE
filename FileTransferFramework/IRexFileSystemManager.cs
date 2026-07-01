using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.FileTransferFramework;

public interface IRexFileSystemManager
{
    Task CopyInputFilesToClusterAsync(SubmittedJobInfo jobSpecification, string localJobDirectory, string sshCaToken, string lexisToken);

    Task<ICollection<JobFileContent>> CopyStdOutputFilesFromClusterAsync(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    Task<ICollection<JobFileContent>> CopyStdErrorFilesFromClusterAsync(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    Task<ICollection<JobFileContent>> CopyProgressFilesFromClusterAsync(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    Task<ICollection<JobFileContent>> CopyLogFilesFromClusterAsync(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    Task<ICollection<JobFileContent>> DownloadPartOfJobFileFromClusterAsync(SubmittedTaskInfo taskSpecification,
        SynchronizableFiles fileType, long offset, string instancePath, string subPath, string sshCaToken, string lexisToken);

    Task CopyCreatedFilesFromClusterAsync(SubmittedJobInfo jobSpecification, DateTime jobSubmitTime, string sshCaToken, string lexisToken);

    Task<ICollection<FileInformation>> ListChangedFilesForJobAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken);
    Task<ICollection<FileInformation>> ListArchivedFilesForJobAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken);

    Task<byte[]> DownloadFileFromClusterAsync(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken, string lexisToken);

    Task<byte[]> DownloadFileFromClusterByAbsolutePathAsync(JobSpecification jobSpecification, string absoluteFilePath, string sshCaToken, string lexisToken);

    Task DeleteSessionFromClusterAsync(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);
    
    Task<bool> UploadFileToClusterByAbsolutePathAsync(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken);
    
    Task<bool> ModifyAbsolutePathFileAttributesAsync(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken,
        bool? ownerCanExecute = null, bool? groupCanExecute = null);
}
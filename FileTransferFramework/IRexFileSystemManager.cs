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
    void CopyInputFilesToCluster(SubmittedJobInfo jobSpecification, string localJobDirectory, string sshCaToken, string lexisToken);

    ICollection<JobFileContent> CopyStdOutputFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    ICollection<JobFileContent> CopyStdErrorFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    ICollection<JobFileContent> CopyProgressFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    ICollection<JobFileContent> CopyLogFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);

    ICollection<JobFileContent> DownloadPartOfJobFileFromCluster(SubmittedTaskInfo taskSpecification,
        SynchronizableFiles fileType, long offset, string instancePath, string subPath, string sshCaToken, string lexisToken);

    void CopyCreatedFilesFromCluster(SubmittedJobInfo jobSpecification, DateTime jobSubmitTime, string sshCaToken, string lexisToken);

    ICollection<FileInformation> ListChangedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken);
    ICollection<FileInformation> ListArchivedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken);

    byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken, string lexisToken);

    byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification, string absoluteFilePath, string sshCaToken, string lexisToken);

    void DeleteSessionFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken, string lexisToken);
    
    bool UploadFileToClusterByAbsolutePath(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken);
    
    bool ModifyAbsolutePathFileAttributes(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken,
        bool? ownerCanExecute = null, bool? groupCanExecute = null);
}
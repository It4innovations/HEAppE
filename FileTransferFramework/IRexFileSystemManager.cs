using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.FileTransferFramework;

public interface IRexFileSystemManager
{
    void CopyInputFilesToCluster(SubmittedJobInfo jobSpecification, string localJobDirectory, string sshCaToken);

    ICollection<JobFileContent> CopyStdOutputFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken);

    ICollection<JobFileContent> CopyStdErrorFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken);

    ICollection<JobFileContent> CopyProgressFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken);

    ICollection<JobFileContent> CopyLogFilesFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken);

    ICollection<JobFileContent> DownloadPartOfJobFileFromCluster(SubmittedTaskInfo taskSpecification,
        SynchronizableFiles fileType, long offset, string instancePath, string subPath, string sshCaToken);

    void CopyCreatedFilesFromCluster(SubmittedJobInfo jobSpecification, DateTime jobSubmitTime, string sshCaToken);

    ICollection<FileInformation> ListChangedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken);
    ICollection<FileInformation> ListArchivedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken);

    byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken);

    byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification, string absoluteFilePath, string sshCaToken);

    void DeleteSessionFromCluster(SubmittedJobInfo jobSpecification, string sshCaToken);
}
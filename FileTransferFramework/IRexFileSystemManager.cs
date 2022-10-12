using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.FileTransferFramework
{
    public interface IRexFileSystemManager
    {
        void CopyInputFilesToCluster(SubmittedJobInfo jobSpecification, string localJobDirectory);

        ICollection<JobFileContent> CopyStdOutputFilesFromCluster(SubmittedJobInfo jobSpecification);

        ICollection<JobFileContent> CopyStdErrorFilesFromCluster(SubmittedJobInfo jobSpecification);

        ICollection<JobFileContent> CopyProgressFilesFromCluster(SubmittedJobInfo jobSpecification);

        ICollection<JobFileContent> CopyLogFilesFromCluster(SubmittedJobInfo jobSpecification);

        ICollection<JobFileContent> DownloadPartOfJobFileFromCluster(SubmittedTaskInfo taskSpecification, SynchronizableFiles fileType, long offset);

        void CopyCreatedFilesFromCluster(SubmittedJobInfo jobSpecification, DateTime jobSubmitTime);

        ICollection<FileInformation> ListChangedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime);

        byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath);

        byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification, string absoluteFilePath);

        void DeleteSessionFromCluster(SubmittedJobInfo jobSpecification);
    }
}
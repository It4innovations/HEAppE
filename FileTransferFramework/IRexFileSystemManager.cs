using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.FileTransferFramework
{
    public interface IRexFileSystemManager
    {
        void CopyInputFilesToCluster(SubmittedJobInfo jobSpecification, string localBasepath, string localJobDirectory);

        ICollection<JobFileContent> CopyStdOutputFilesFromCluster(SubmittedJobInfo jobSpecification, string localBasepath);

        ICollection<JobFileContent> CopyStdErrorFilesFromCluster(SubmittedJobInfo jobSpecification, string localBasepath);

        ICollection<JobFileContent> CopyProgressFilesFromCluster(SubmittedJobInfo jobSpecification, string localBasepath);

        ICollection<JobFileContent> CopyLogFilesFromCluster(SubmittedJobInfo jobSpecification, string localBasepath);

        ICollection<JobFileContent> DownloadPartOfJobFileFromCluster(SubmittedTaskInfo taskSpecification, SynchronizableFiles fileType, long offset, string localBasepath);

        void CopyCreatedFilesFromCluster(SubmittedJobInfo jobSpecification, string localBasepath, DateTime jobSubmitTime);

        ICollection<FileInformation> ListChangedFilesForJob(SubmittedJobInfo jobInfo, string localBasepath, DateTime jobSubmitTime);

        byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string localBasepath, string relativeFilePath);

        byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification, string absoluteFilePath);

        void DeleteSessionFromCluster(SubmittedJobInfo jobSpecification, string localBasepath);
    }
}
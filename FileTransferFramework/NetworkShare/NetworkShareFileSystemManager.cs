using System;
using System.Collections.Generic;
using System.IO;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.MiddlewareUtils;
using Microsoft.Extensions.Logging;

namespace HEAppE.FileTransferFramework.NetworkShare
{
    public class NetworkShareFileSystemManager : AbstractFileSystemManager
    {
        #region Constructors
        public NetworkShareFileSystemManager(ILogger logger, FileTransferMethod configuration, FileSystemFactory synchronizerFactory)
            : base(logger, configuration, synchronizerFactory)
        {

        }
        #endregion
        #region AbstractFileSystemManager Members
        public override byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath)
        {
            throw new NotImplementedException();
        }

        public override void DeleteSessionFromCluster(SubmittedJobInfo jobInfo)
        {
            string jobClusterDirectoryPath = FileSystemUtils.GetJobClusterDirectoryPath(_fileSystem.Cluster.LocalBasepath, jobInfo.Specification);
            UnsetReadOnlyForAllFiles(jobClusterDirectoryPath);
            Directory.Delete(jobClusterDirectoryPath, true);
        }

        protected override void CopyAll(string source, string target, bool overwrite, DateTime? lastModificationLimit,
            string[] excludedFiles, ClusterAuthenticationCredentials credentials)
        {
            FileSystemUtils.CopyAll(source, target, overwrite, lastModificationLimit, excludedFiles);
        }

        protected override ICollection<FileInformation> ListChangedFilesForTask(string taskClusterDirectoryPath, DateTime? jobSubmitTime, ClusterAuthenticationCredentials clusterAuthenticationCredentials)
        {
            throw new NotImplementedException();
        }

        protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo, ClusterAuthenticationCredentials credentials)
        {
            return _synchronizerFactory.CreateFileSynchronizer(fileInfo, credentials);
        }
        #endregion
        #region Local Methods
        private static void UnsetReadOnlyForAllFiles(string dirPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    file.IsReadOnly = false;
                }
                catch (Exception)
                {
                }
            }
        }
        #endregion
    }
}
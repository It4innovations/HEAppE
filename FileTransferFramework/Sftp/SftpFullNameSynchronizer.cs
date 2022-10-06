using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Utils;
using Renci.SshNet;
using System.Collections.Generic;
using System.IO;
using ConnectionInfo = HEAppE.ConnectionPool.ConnectionInfo;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class SftpFullNameSynchronizer : IFileSynchronizer
    {
        #region Instances
        private readonly ClusterAuthenticationCredentials _credentials;
        #endregion
        #region Properties
        public IConnectionPool ConnectionPool { get; set; }
        public FullFileSpecification SyncFileInfo { get; set; }
        public long Offset { get; set; }
        #endregion
        #region Constructors
        public SftpFullNameSynchronizer(FullFileSpecification syncFile, ClusterAuthenticationCredentials credentials)
        {
            _credentials = credentials;
            SyncFileInfo = syncFile;
            Offset = 0;
        }
        #endregion
        #region Methods
        public ICollection<JobFileContent> SynchronizeFiles(long clusterId, long projectId)
        {
            ConnectionInfo connection = ConnectionPool.GetConnectionForUser(_credentials, _credentials.GetClusterForProject(clusterId, projectId));
            try
            {
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                string sourcePath = FileSystemUtils.ConcatenatePaths(SyncFileInfo.SourceDirectory, SyncFileInfo.RelativePath);

                if (client.Exists(sourcePath))
                {
                    using Stream sourceStream = client.OpenRead(sourcePath);
                    string synchronizedContent = FileSystemUtils.ReadStreamContentFromSpecifiedOffset(sourceStream, Offset);
                    if (!string.IsNullOrEmpty(SyncFileInfo.DestinationDirectory))
                    {
                        string destinationPath = Path.Combine(SyncFileInfo.DestinationDirectory, SyncFileInfo.RelativePath);
                        if (Offset == 0)
                        {
                            File.Delete(destinationPath);
                        }
                        FileSystemUtils.WriteStringToLocalFile(synchronizedContent, destinationPath);
                    }

                    JobFileContent[] result = new JobFileContent[1]
                    {
                        new JobFileContent {
                            RelativePath = SyncFileInfo.RelativePath,
                            Content = synchronizedContent,
                            Offset = Offset
                        }
                    };

                    if (SyncFileInfo.SynchronizationType == FileSynchronizationType.IncrementalAppend)
                    {
                        Offset = sourceStream.Position;
                    }
                    return result;
                }
            }
            finally
            {
                ConnectionPool.ReturnConnection(connection);
            }
            return default;
        }
        #endregion
    }
}
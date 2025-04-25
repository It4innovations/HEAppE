using System.Collections.Generic;
using System.IO;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Utils;
using Renci.SshNet;

namespace HEAppE.FileTransferFramework.Sftp;

public class SftpFullNameSynchronizer : IFileSynchronizer
{
    #region Instances

    private readonly ClusterAuthenticationCredentials _credentials;

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

    public ICollection<JobFileContent> SynchronizeFiles(Cluster cluster)
    {
        var connection = ConnectionPool.GetConnectionForUser(_credentials, cluster);
        try
        {
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            var sourcePath = FileSystemUtils.ConcatenatePaths(SyncFileInfo.SourceDirectory, SyncFileInfo.RelativePath);

            if (client.Exists(sourcePath))
            {
                using var sourceStream = client.OpenRead(sourcePath);
                var synchronizedContent = FileSystemUtils.ReadStreamContentFromSpecifiedOffset(sourceStream, Offset);
                if (!string.IsNullOrEmpty(SyncFileInfo.DestinationDirectory))
                {
                    var destinationPath = Path.Combine(SyncFileInfo.DestinationDirectory, SyncFileInfo.RelativePath);
                    if (Offset == 0) File.Delete(destinationPath);
                    FileSystemUtils.WriteStringToLocalFile(synchronizedContent, destinationPath);
                }

                var result = new JobFileContent[1]
                {
                    new()
                    {
                        RelativePath = SyncFileInfo.RelativePath,
                        Content = synchronizedContent,
                        Offset = Offset
                    }
                };

                if (SyncFileInfo.SynchronizationType == FileSynchronizationType.IncrementalAppend)
                    Offset = sourceStream.Position;
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

    #region Properties

    public IConnectionPool ConnectionPool { get; set; }
    public FullFileSpecification SyncFileInfo { get; set; }
    public long Offset { get; set; }

    #endregion
}
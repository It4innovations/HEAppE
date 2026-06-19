using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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


    public async Task<ICollection<JobFileContent>> SynchronizeFilesAsync(Cluster cluster, string sshCaToken, string lexisToken)
    {
        var connection = await ConnectionPool.GetConnectionForUserAsync(_credentials, cluster, sshCaToken, lexisToken);
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            var sourcePath = FileSystemUtils.ConcatenatePaths(SyncFileInfo.SourceDirectory, SyncFileInfo.RelativePath);

            if (await client.ExistsAsync(sourcePath))
            {
                using var sourceStream = await client.OpenReadAsync(sourcePath);
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
            await ConnectionPool.ReturnConnectionAsync(connection);
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
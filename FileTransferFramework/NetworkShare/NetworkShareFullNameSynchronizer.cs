using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Utils;
using System.Collections.Generic;
using System.IO;

namespace HEAppE.FileTransferFramework.NetworkShare
{
    public class NetworkShareFullNameSynchronizer : IFileSynchronizer
    {
        #region Properties
        public FullFileSpecification SyncFileInfo { get; set; }
        public long Offset { get; set; }
        #endregion
        #region Constructors
        public NetworkShareFullNameSynchronizer(FullFileSpecification syncFile)
        {
            SyncFileInfo = syncFile;
            Offset = 0;
        }
        #endregion
        #region Local Methods
        public ICollection<JobFileContent> SynchronizeFiles(long clusterId, long projectId)
        {
            if (File.Exists(Path.Combine(SyncFileInfo.SourceDirectory, SyncFileInfo.RelativePath)))
            {
                using Stream sourceStream = new FileStream(Path.Combine(SyncFileInfo.SourceDirectory, SyncFileInfo.RelativePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string destinationPath = Path.Combine(SyncFileInfo.DestinationDirectory, SyncFileInfo.RelativePath);
                if (Offset == 0)
                {
                    File.Delete(destinationPath);
                }
                else
                {
                    sourceStream.Seek(Offset, SeekOrigin.Begin);
                }
                string synchronizedContent = FileSystemUtils.WriteStreamToLocalFile(sourceStream, destinationPath);
                if (SyncFileInfo.SynchronizationType == FileSynchronizationType.IncrementalAppend)
                {
                    Offset = sourceStream.Position;
                }
                JobFileContent[] result = {
                        new JobFileContent
                        {
                            RelativePath = SyncFileInfo.RelativePath,
                            Content = synchronizedContent
                        }
                    };
                return result;
            }
            else
            {
                return default;
            }
        }
        #endregion
    }
}
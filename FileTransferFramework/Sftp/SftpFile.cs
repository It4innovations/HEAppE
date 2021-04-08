using System;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class SftpFile
    {
        #region Properties
        public string Name { get; internal set; }
        public bool IsDirectory { get; internal set; }
        public string FullName { get; internal set; }
        public DateTime LastWriteTime { get; internal set; }
        public bool IsSymbolicLink { get; internal set; }
        #endregion
    }
}

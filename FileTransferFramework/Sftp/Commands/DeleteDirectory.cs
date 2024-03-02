using HEAppE.Exceptions.Internal;
using System;

namespace HEAppE.FileTransferFramework.Sftp.Commands
{
    internal class DeleteDirectory : ICommand
    {
        #region Instances
        private readonly string _remotePath;
        #endregion
        #region Properties
        public string Command => $"rmdir {_remotePath}";
        #endregion
        #region Constructors
        public DeleteDirectory(string remotePath)
        {
            _remotePath = remotePath;
        }
        #endregion
        #region Methods
        public void ProcessResult(SftpCommandResult result)
        {
            throw new SftpClientException("NotSupportedAuthentication", "ProcessResult");
        }
        #endregion
    }
}

﻿using System;

namespace HEAppE.FileTransferFramework.Sftp.Commands
{
    internal class DeleteFile : ICommand
    {
        #region Instances
        private readonly string _remotePath;
        #endregion
        #region Properties
        public string Command => $"rm {_remotePath}";
        #endregion
        #region Constructors

        public DeleteFile(string remotePath)
        {
            _remotePath = remotePath;
        }
        #endregion
        #region Methods
        public void ProcessResult(SftpCommandResult result)
        {
            throw new Exception("ProcessResult method is not defined!");
        }
        #endregion
    }
}

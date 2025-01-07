using System;
using System.IO;

namespace HEAppE.FileTransferFramework.Sftp.Commands;

internal class DownloadFile : ICommand
{
    #region Constructors

    public DownloadFile(string remotePath, MemoryStream stream)
    {
        _remotePath = remotePath;
        _stream = stream;
    }

    #endregion

    #region Properties

    public string Command => $"get {_remotePath} {_localFile}";

    #endregion

    #region Methods

    public void ProcessResult(SftpCommandResult result)
    {
        using (var fs = File.OpenRead(_localFile))
        {
            fs.CopyTo(_stream);
        }

        _stream.Position = 0;
        File.Delete(_localFile);
    }

    #endregion

    #region Instances

    private readonly string _remotePath;
    private readonly MemoryStream _stream;
    private readonly string _localFile = $"/sftp/{Guid.NewGuid()}";

    #endregion
}
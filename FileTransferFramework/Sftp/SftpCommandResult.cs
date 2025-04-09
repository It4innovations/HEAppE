namespace HEAppE.FileTransferFramework.Sftp;

public class SftpCommandResult
{
    #region Properties

    public string CommandText { get; internal set; }
    public int ExitStatus { get; internal set; }
    public string Error { get; internal set; }
    public string Output { get; internal set; }

    #endregion
}
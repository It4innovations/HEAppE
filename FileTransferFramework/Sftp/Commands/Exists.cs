namespace HEAppE.FileTransferFramework.Sftp.Commands;

internal class Exists : ICommand<bool>
{
    #region Instances

    private readonly string _remotePath;

    #endregion

    #region Constructors

    public Exists(string remotePath)
    {
        _remotePath = remotePath;
    }

    #endregion

    #region Properties

    public string Command => "ls " + _remotePath;

    #endregion

    #region Methods

    public bool ProcessResult(SftpCommandResult result)
    {
        return result.ExitStatus == 0;
    }

    #endregion
}
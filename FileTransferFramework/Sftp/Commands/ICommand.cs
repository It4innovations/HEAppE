namespace HEAppE.FileTransferFramework.Sftp.Commands
{
    public interface ICommand<T>
    {
        string Command { get; }

        T ProcessResult(string remoteNodeTimeZone, SftpCommandResult result);
    }

    public interface ICommand
    {
        string Command { get; }

        void ProcessResult(string remoteNodeTimeZone, SftpCommandResult result);
    }
}

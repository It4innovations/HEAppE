namespace HEAppE.FileTransferFramework.Sftp.Commands;

public interface ICommand<T>
{
    string Command { get; }

    T ProcessResult(SftpCommandResult result);
}

public interface ICommand
{
    string Command { get; }

    void ProcessResult(SftpCommandResult result);
}
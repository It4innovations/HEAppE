using System.IO;
using System.Text;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh client adapter
/// </summary>
public class SshClientAdapter
{
    #region Instances

    private readonly SshClient _sshClient;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="sshClient">Ssh client</param>
    public SshClientAdapter(SshClient sshClient)
    {
        _sshClient = sshClient;
    }

    #endregion

    #region Local Methods

    /// <summary>
    ///     Run command
    /// </summary>
    /// <param name="command">Command</param>
    /// <returns></returns>
    public SshCommandWrapper RunCommand(string command)
    {
        if (_sshClient is NoAuthenticationSshClient ownSshCommand)
            return ownSshCommand.RunShellCommand(command);
        
        //return new SshCommandWrapper(_sshClient.RunCommand(command));
        var output = RunBigCommand(command, Encoding.UTF8);
        return new SshCommandWrapper
        {
            Result = output,
            CommandText = command,
            ExitStatus = 0,
            Error = string.Empty
        };
    }
    
    private string RunBigCommand(string commandText, Encoding encoding, int readDelayMs = 100)
    {
        using var input = new MemoryStream(encoding.GetBytes(commandText + "\n"));
        using var output = new MemoryStream();
        using var error = new MemoryStream();

        using var shell = _sshClient.CreateShellNoTerminal(input, output, error);

        var result = new StringBuilder();
        var buffer = new byte[8192];
        
        int bytesRead;
        while ((bytesRead = output.Read(buffer, 0, buffer.Length)) > 0)
        {
            result.Append(encoding.GetString(buffer, 0, bytesRead));
            System.Threading.Thread.Sleep(readDelayMs);
        }
        
        output.Seek(0, SeekOrigin.Begin);
        error.Seek(0, SeekOrigin.Begin);

        return result.ToString();
    }



    /// <summary>
    ///     Connect
    /// </summary>
    public void Connect()
    {
        if (_sshClient is not NoAuthenticationSshClient) _sshClient.Connect();
    }

    /// <summary>
    ///     Disconnect
    /// </summary>
    public void Disconnect()
    {
        if (_sshClient is not NoAuthenticationSshClient) _sshClient.Disconnect();
    }

    #endregion
}
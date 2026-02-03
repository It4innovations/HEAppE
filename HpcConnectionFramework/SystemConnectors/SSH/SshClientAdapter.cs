using System;
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
        
        return new SshCommandWrapper(_sshClient.RunCommand(command));
    }



    /// <summary>
    ///     Connect
    /// </summary>
    public void Connect()
    {
        _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(30);
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
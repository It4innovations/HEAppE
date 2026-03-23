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
        
        if (_sshClient is KerberosSshClient krbSshCommand)
            return krbSshCommand.RunCommand(command);
        
        using var cmd = _sshClient.CreateCommand(command);
        cmd.Execute();

        return new SshCommandWrapper(cmd);
    }

    /// <summary>
    ///     Connect
    /// </summary>
    public void Connect()
    {
        _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(30);
        
        switch (_sshClient)
        {
            case NoAuthenticationSshClient:
                break;
            case KerberosSshClient krbClient:
                krbClient.Connect();
                break;
            default:
                _sshClient.Connect();
                break;
        }
    }

    /// <summary>
    ///     Disconnect
    /// </summary>
    public void Disconnect()
    {
        switch (_sshClient)
        {
            case NoAuthenticationSshClient:
                break;
            case KerberosSshClient krbClient:
                krbClient.Disconnect();
                break;
            default:
                _sshClient.Disconnect();
                break;
        }
    }

    #endregion
}
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh client adapter
/// </summary>
public class SshClientAdapter
{
    #region Instances

    private readonly SshClient _sshClient;

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, string> CommandPrefixes = new();

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

    public string Host => _sshClient?.ConnectionInfo?.Host;
    public int? Port => _sshClient?.ConnectionInfo?.Port;

    public static void RegisterCommandPrefix(object sshClient, string prefix)
    {
        if (sshClient != null && !string.IsNullOrEmpty(prefix))
        {
            CommandPrefixes.AddOrUpdate(sshClient, prefix);
        }
    }

    #region Local Methods



    /// <summary>
    ///     Run command async
    /// </summary>
    /// <param name="command">Command</param>
    /// <returns></returns>
    public async Task<SshCommandWrapper> RunCommandAsync(string command)
    {
        if (CommandPrefixes.TryGetValue(_sshClient, out var prefix) && !string.IsNullOrEmpty(prefix))
        {
            command = $"{prefix} && {command}";
        }

        if (_sshClient is NoAuthenticationSshClient ownSshCommand)
            return await Task.Run(() => ownSshCommand.RunShellCommand(command));
        
        if (_sshClient is KerberosSshClient krbSshCommand)
            return await krbSshCommand.ExecuteAsync(command);
        
        using var cmd = _sshClient.CreateCommand(command);
        await Task.Factory.FromAsync(cmd.BeginExecute(), cmd.EndExecute);
        return new SshCommandWrapper(cmd);
    }



    /// <summary>
    ///     Connect async
    /// </summary>
    public async Task ConnectAsync()
    {
        switch (_sshClient)
        {
            case NoAuthenticationSshClient:
                break;
            case KerberosSshClient krbClient:
                await krbClient.ConnectAsync();
                break;
            default:
                await Task.Run(() => _sshClient.Connect());
                break;
        }

        // Set keep-alive interval after successful connection
        _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    ///     Disconnect async
    /// </summary>
    public async Task DisconnectAsync()
    {
        switch (_sshClient)
        {
            case NoAuthenticationSshClient:
                break;
            case KerberosSshClient krbClient:
                krbClient.Disconnect();
                break;
            default:
                await Task.Run(() => _sshClient.Disconnect());
                break;
        }
    }

    #endregion
}
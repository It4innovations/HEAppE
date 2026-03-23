using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using HEAppE.Exceptions.Internal;
using log4net;
using Renci.SshNet;
using Tmds.Ssh;
using TmdsClient = Tmds.Ssh.SshClient;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh agent client
/// </summary>
public class KerberosSshClient : Renci.SshNet.SshClient
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="port"></param>
    /// <param name="userName">Username</param>
    /// <exception cref="ArgumentException"></exception>
    public KerberosSshClient(string masterNodeName, string address, string userName) : base(
        new ConnectionInfo(masterNodeName, userName,
            new NoneAuthenticationMethod(userName))) //cannot be null
    {
        //TODO: masterNodeName?
        if (string.IsNullOrWhiteSpace(masterNodeName))
            throw new SshClientArgumentException("NullArgument", "masterNodeName");

        if (string.IsNullOrWhiteSpace(userName)) 
        throw new SshClientArgumentException("NullArgument", "userName");

        _masterNodeName = masterNodeName;
        _address = address;
        _userName = userName;

        _log = LogManager.GetLogger(typeof(KerberosSshClient));

        _client = InitializeClient(address, userName);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Master node name
    /// </summary>
    private readonly string _masterNodeName;

    /// <summary>
    ///     Port
    /// </summary>
    private readonly string _address;

    /// <summary>
    ///     Username
    /// </summary>
    private readonly string _userName;

    /// <summary>
    ///     TmdsSshClient
    /// </summary>
    private readonly TmdsClient _client;

    /// <summary>
    ///     Log4Net logger
    /// </summary>
    protected ILog _log;

    #endregion

    #region Methods

    /// <summary>
    /// Initializes and returns a new Tmds.ssh.SshClient.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="userName"></param>
    private TmdsClient InitializeClient(string address, string userName)
    {
        var sshConfigSettings = new SshConfigSettings();
        // clears SshConfigOption.SendEnv and others.
        sshConfigSettings.ConfigFilePaths.Clear();
        sshConfigSettings.Options.Add(SshConfigOption.User, new SshConfigOptionValue(userName));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIAuthentication, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIDelegateCredentials, new SshConfigOptionValue("yes"));
        // StrictHostKeyChecking = "no" -> removes the need for known_hosts file key
        //TODO: this setting should be set as HEappE static option
        sshConfigSettings.Options.Add(SshConfigOption.StrictHostKeyChecking, new SshConfigOptionValue("no"));

        return new TmdsClient(address, sshConfigSettings);
    }

    //TODO:
    //property KeepAliveInterval

    //sshClient.ConnectionInfo.RetryAttempts
    //sshClient.ConnectionInfo.Timeout
    
    /// <summary>
    /// SshClient connects asynchronously.
    /// </summary>
    private Task ConnectAsync()
    {
        return _client.ConnectAsync();
    }

    /// <summary>
    /// Execute a command asynchronously.
    /// </summary>
    /// <param name="commandText"></param>
    private async Task<SshCommandWrapper> ExecuteAsync(string commandText)
    {
        using (var process = await _client.ExecuteAsync(commandText))
        {
            var result = new SshCommandWrapper { CommandText = commandText };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            while (true)
            {
                var (isError, line) = await process.ReadLineAsync();
                if (line == null) break;

                if (isError) stderr.AppendLine(line);
                else stdout.AppendLine(line);
            }

            // Wait for process completion to get exit status
            await process.WaitForExitAsync();
            
            result.Result = stdout.ToString().TrimEnd();
            result.Error = stderr.ToString().TrimEnd();
            result.ExitStatus = process.ExitCode;
            
            return result;
        }
    }

    /// <summary>
    /// Start SshClient connection.
    /// </summary>
    public new void Connect()
    {
        ConnectAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Run a remote command.
    /// </summary>
    /// <param name="commandText"></param>
    public new SshCommandWrapper RunCommand(string commandText)
    {
        return ExecuteAsync(commandText).GetAwaiter().GetResult();
    }

    /// <summary>
    /// End SshClient connection.
    /// </summary>
    public new void Disconnect()
    {
        _client.Dispose();
    }

    #endregion
}
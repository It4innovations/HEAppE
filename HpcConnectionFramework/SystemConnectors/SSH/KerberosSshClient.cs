using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using HEAppE.Exceptions.Internal;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Tmds.Ssh;
using TmdsClient = Tmds.Ssh.SshClient;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Kerberos Ssh client
/// </summary>
public class KerberosSshClient : Renci.SshNet.SshClient
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="address"></param>
    /// <param name="userName">Username</param>
    /// <exception cref="ArgumentException"></exception>
    public KerberosSshClient(string masterNodeName, string address, string userName, ILogger logger = null) : base(
        new ConnectionInfo(masterNodeName, userName,
            new NoneAuthenticationMethod(userName))) //cannot be null
    {
        if (string.IsNullOrWhiteSpace(masterNodeName))
            throw new SshClientArgumentException("NullArgument", "masterNodeName");

        if (string.IsNullOrWhiteSpace(userName)) 
            throw new SshClientArgumentException("NullArgument", "userName");

        _masterNodeName = masterNodeName;
        _address = address;
        _userName = userName;

        _log = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        InitializeClient();
    }

    #endregion

    #region Instances

    private readonly string _masterNodeName;
    private readonly string _address;
    private readonly string _userName;
    private TmdsClient _client;
    protected ILogger _log;
    private bool _triedToConnect = false;
    private bool _isConnected = false;

    #endregion

    #region Methods

    private void InitializeClient()
    {
        var sshConfigSettings = new SshConfigSettings();
        sshConfigSettings.ConfigFilePaths.Clear();
        sshConfigSettings.Options.Add(SshConfigOption.User, new SshConfigOptionValue(_userName));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIAuthentication, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIDelegateCredentials, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.StrictHostKeyChecking, new SshConfigOptionValue("no"));

        _client = new TmdsClient(_address, sshConfigSettings);
        _triedToConnect = false;
        _isConnected = false;
    }

    public override bool IsConnected => _isConnected && !_client.Disconnected.IsCancellationRequested;

    public async Task ConnectAsync()
    {
        if (IsConnected) return;

        if (_triedToConnect)
        {
            _client.Dispose();
            InitializeClient();
        }

        _triedToConnect = true;
        await _client.ConnectAsync();
        _isConnected = true;
    }

    public async Task<SshCommandWrapper> ExecuteAsync(string commandText)
    {
        using (var process = await _client.ExecuteAsync(commandText))
        {
            var (stdout, stderr) = await process.ReadToEndAsStringAsync();
            
            return new SshCommandWrapper
            {
                CommandText = commandText,
                Result = stdout.TrimEnd(),
                Error = stderr.TrimEnd(),
                ExitStatus = process.ExitCode
            };
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _client.Dispose();
    }

    #endregion
}
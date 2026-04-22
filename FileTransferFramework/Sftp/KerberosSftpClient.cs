using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Tmds.Ssh;
using TmdsSftpClient = Tmds.Ssh.SftpClient;

namespace HEAppE.FileTransferFramework.Sftp;

public class KerberosSftpClient : Renci.SshNet.SftpClient
{
    private Tmds.Ssh.SshClient _sshClient;
    private TmdsSftpClient _sftpClient;
    private readonly string _userName;
    private readonly string _address;
    private readonly ILogger _logger;
    private bool _triedToConnect = false;
    private bool _isConnected = false;

    public KerberosSftpClient(ILogger logger, string masterNodeName, string address, string userName)
        : base(new ConnectionInfo(address, userName, new NoneAuthenticationMethod(userName)))
    {
        _logger = logger;
        _userName = userName;
        _address = address;
        
        InitializeInternalClients();
    }

    private void InitializeInternalClients()
    {
        var sshConfigSettings = new SshConfigSettings();
        sshConfigSettings.ConfigFilePaths.Clear();
        sshConfigSettings.Options.Add(SshConfigOption.User, new SshConfigOptionValue(_userName));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIAuthentication, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIDelegateCredentials, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.StrictHostKeyChecking, new SshConfigOptionValue("no"));

        _sshClient = new Tmds.Ssh.SshClient(_address, sshConfigSettings);
        _sftpClient = new TmdsSftpClient(_sshClient);
        _triedToConnect = false;
        _isConnected = false;
    }

    public override bool IsConnected => _isConnected && !_sshClient.Disconnected.IsCancellationRequested;

    public new async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return;

        if (_triedToConnect)
        {
            _sshClient.Dispose();
            InitializeInternalClients();
        }

        _triedToConnect = true;
        await _sshClient.ConnectAsync(ct);
        _isConnected = true;
    }

    public new string WorkingDirectory {
        get {
            try {
                return _sftpClient.WorkingDirectory.Path;
            } catch {
                return string.Empty;
            }
        }
    }

    public new async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        try {
            var attributes = await _sftpClient.GetAttributesAsync(path, followLinks: true, filter: null, ct);
            return attributes != null;
        } catch {
            return false;
        }
    }

    public new async Task DownloadFileAsync(string remotePath, Stream stream, CancellationToken ct = default)
    {
        await _sftpClient.DownloadFileAsync(remotePath, stream, ct);
    }

    public async Task<List<HEAppE.FileTransferFramework.Sftp.SftpFile>> ListDirectoryAsync(string hostTimeZone, string remotePath, CancellationToken ct = default)
    {
        var entries = _sftpClient.GetDirectoryEntriesAsync<HEAppE.FileTransferFramework.Sftp.SftpFile>(remotePath, (ref SftpFileEntry entry) => new HEAppE.FileTransferFramework.Sftp.SftpFile
        {
            Name = entry.FileName.ToString(),
            FullName = entry.ToPath(),
            IsDirectory = entry.FileType == UnixFileType.Directory,
            IsSymbolicLink = entry.FileType == UnixFileType.SymbolicLink,
            LastWriteTime = entry.LastWriteTime.LocalDateTime.Convert(hostTimeZone)
        });
        
        var result = new List<HEAppE.FileTransferFramework.Sftp.SftpFile>();
        await foreach (var entry in entries.WithCancellation(ct))
        {
            result.Add(entry);
        }
        return result;
    }

    public new async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        await _sftpClient.DeleteFileAsync(path, ct);
    }

    public new async Task DeleteDirectoryAsync(string path, CancellationToken ct = default)
    {
        await _sftpClient.DeleteDirectoryAsync(path, recursive: true, ct);
    }

    public async Task RenameAsync(string oldPath, string newPath, CancellationToken ct = default)
    {
        await _sftpClient.RenameAsync(oldPath, newPath, cancellationToken: ct);
    }

    public new void RenameFile(string oldPath, string newPath)
    {
        RenameAsync(oldPath, newPath).GetAwaiter().GetResult();
    }

    public new async Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        await _sftpClient.CreateDirectoryAsync(path, createParents: true, cancellationToken: ct);
    }

    public new async Task UploadFileAsync(Stream source, string path, bool canOverride, CancellationToken ct = default)
    {
        await _sftpClient.UploadFileAsync(source, path, overwrite: canOverride, cancellationToken: ct);
    }
    
    public new async Task<SftpFileAttributes> GetAttributesAsync(string path, CancellationToken ct = default)
    {
        return null;
    }

    public async Task SetAttributesAsync(string path, SftpFileAttributes attributes, CancellationToken ct = default)
    {
        await _sftpClient.SetAttributesAsync(path, cancellationToken: ct);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await _sftpClient.DownloadFileAsync(path, ms, ct);
        ms.Position = 0;
        return ms;
    }

    public void ConnectSync() => ConnectAsync().GetAwaiter().GetResult();
    public void DisconnectSync() 
    {
        _isConnected = false;
        _sshClient.Dispose();
    }
}

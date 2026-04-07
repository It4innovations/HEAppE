using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework.Sftp.Commands;
using HEAppE.Utils;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace HEAppE.FileTransferFramework.Sftp;

public class SftpClientAdapter
{
    #region Instances

    private readonly SftpClient _sftpClient;

    #endregion

    #region Constructors

    public SftpClientAdapter(SftpClient sftpClient)
    {
        _sftpClient = sftpClient;
    }

    #endregion

    #region Properties

    internal string WorkingDirectory
    {
        get
        {
            if (_sftpClient is KerberosSftpClient kerberosSftpClient)
                return kerberosSftpClient.WorkingDirectory;
            return _sftpClient.WorkingDirectory;
        }
    }

    #endregion

    #region Methods

    #region Methods

    internal async Task ConnectAsync()
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient) await kerberosSftpClient.ConnectAsync();
        else if (_sftpClient is not NoAuthenticationSftpClient) await Task.Run(() => _sftpClient.Connect());
    }

    internal async Task DisconnectAsync()
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient) kerberosSftpClient.DisconnectSync(); // Dispose-based
        else if (_sftpClient is not NoAuthenticationSftpClient) await Task.Run(() => _sftpClient.Disconnect());
    }

    internal async Task<bool> ExistsAsync(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            return noAuthenticationSftpClient.RunCommand(new Exists(remotePath));
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            return await kerberosSftpClient.ExistsAsync(remotePath);
        return await Task.Run(() => _sftpClient.Exists(remotePath));
    }

    internal async Task DownloadFileAsync(string remotePath, MemoryStream stream)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DownloadFile(remotePath, stream));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.DownloadFileAsync(remotePath, stream);
        else
            await Task.Run(() => _sftpClient.DownloadFile(remotePath, stream));
    }

    internal async Task<IEnumerable<SftpFile>> ListDirectoryAsync(string hostTimeZone, string remotePath)
    {
        var items = new List<SftpFile>();
        switch (_sftpClient)
        {
            case NoAuthenticationSftpClient noAuthenticationSftpClient:
                var remoteWorkingDirectory = noAuthenticationSftpClient.RunCommand(new WorkingDirectory());
                var result =
                    noAuthenticationSftpClient.RunCommand(new ListDirectory(hostTimeZone, remotePath,
                        remoteWorkingDirectory));
                items.AddRange(result);
                break;

            case KerberosSftpClient kerberosSftpClient:
                items.AddRange(await kerberosSftpClient.ListDirectoryAsync(hostTimeZone, remotePath));
                break;

            case SftpClient sftpClient:
            {
                try
                {
                    bool replacedTilde = false;
                    if (remotePath.StartsWith("~/"))
                    {
                        remotePath = remotePath.Replace("~", sftpClient.WorkingDirectory);
                        replacedTilde = true;
                    }
                    var resultItems = await Task.Run(() => sftpClient.ListDirectory(remotePath));
                    foreach (var item in resultItems)
                    {
                        items.Add(new SftpFile
                        {
                            FullName = replacedTilde ? item.FullName.Replace(sftpClient.WorkingDirectory, "~") : item.FullName,
                            IsDirectory = item.IsDirectory,
                            IsSymbolicLink = item.IsSymbolicLink,
                            LastWriteTime = item.LastWriteTime.Convert(hostTimeZone),
                            Name = item.Name
                        });
                    }
                        
                }
                catch (SshException exception)
                {
                    //if directory is empty 'No such file' exception is raised, handle that case
                    switch (exception.Message)
                    {
                        case "No such file":
                            break;
                        default:
                            throw;
                    }
                }
            }
                break;
            default:
            {
                var resultItems = await Task.Run(() => _sftpClient.ListDirectory(remotePath));
                foreach (var item in resultItems)
                    items.Add(new SftpFile
                    {
                        FullName = item.FullName,
                        IsDirectory = item.IsDirectory,
                        IsSymbolicLink = item.IsSymbolicLink,
                        LastWriteTime = item.LastWriteTimeUtc,
                        Name = item.Name
                    });
            }
                break;
        }

        return items;
    }

    internal async Task DeleteAsync(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath= remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.DeleteFileAsync(remotePath);
        else
            await Task.Run(() => _sftpClient.Delete(remotePath));
    }

    internal async Task DeleteFileAsync(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.DeleteFileAsync(remotePath);
        else
            await Task.Run(() => _sftpClient.DeleteFile(remotePath));
    }

    internal async Task DeleteDirectoryAsync(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteDirectory(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.DeleteDirectoryAsync(remotePath);
        else
            await Task.Run(() => _sftpClient.DeleteDirectory(remotePath));
    }

    internal async Task DownloadFileAsync(string fullName, FileStream targetStream)
    {
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "download file");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.DownloadFileAsync(fullName, targetStream);
        else
            await Task.Run(() => _sftpClient.DownloadFile(fullName, targetStream));
    }

    internal async Task CreateDirectoryAsync(string targetPath)
    {
        if (targetPath.StartsWith("~/"))
        {
            targetPath = targetPath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "create directory");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.CreateDirectoryAsync(targetPath);
        else
            await Task.Run(() => _sftpClient.CreateDirectory(targetPath));
    }

    internal async Task UploadFileAsync(Stream sourceStream, string targetFilePath, bool canOverride)
    {
        if (targetFilePath.StartsWith("~/"))
        {
            targetFilePath = targetFilePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "upload file");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.UploadFileAsync(sourceStream, targetFilePath, canOverride);
        else
            await Task.Run(() => _sftpClient.UploadFile(sourceStream, targetFilePath, canOverride));
    }

    internal async Task RenameFileAsync(string oldPath, string newPath)
    {
        if (oldPath.StartsWith("~/"))
        {
            oldPath = oldPath.Replace("~", WorkingDirectory);
        }
        if (newPath.StartsWith("~/"))
        {
            newPath = newPath.Replace("~", WorkingDirectory);
        }

        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "rename file");
        
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.RenameAsync(oldPath, newPath);
        else
            await Task.Run(() => _sftpClient.RenameFile(oldPath, newPath));
    }
    
    internal async Task<SftpFileAttributes> GetFileAttributesAsync(string path)
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            return await kerberosSftpClient.GetAttributesAsync(path);
        return await Task.Run(() => _sftpClient.GetAttributes(path));
    }

    internal async Task SetFileAttributesAsync(string path, SftpFileAttributes fileAttributes)
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            await kerberosSftpClient.SetAttributesAsync(path, fileAttributes);
        else
            await Task.Run(() => _sftpClient.SetAttributes(path, fileAttributes));
    }

    internal async Task<Stream> OpenReadAsync(string path)
    {
        if (path.StartsWith("~/"))
        {
            path = path.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
        {
            var ms = new MemoryStream();
            noAuthenticationSftpClient.RunCommand(new DownloadFile(path, ms));
            return ms;
        }

        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
        {
            return await kerberosSftpClient.OpenReadAsync(path);
        }

        return await Task.Run(() => _sftpClient.OpenRead(path));
    }

    #endregion

    #endregion
}
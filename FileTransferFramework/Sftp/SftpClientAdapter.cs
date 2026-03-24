using System.Collections.Generic;
using System.IO;
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

    internal void Connect()
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient) kerberosSftpClient.ConnectSync();
        else if (_sftpClient is not NoAuthenticationSftpClient) _sftpClient.Connect();
    }

    internal void Disconnect()
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient) kerberosSftpClient.DisconnectSync();
        else if (_sftpClient is not NoAuthenticationSftpClient) _sftpClient.Disconnect();
    }

    internal bool Exists(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            return noAuthenticationSftpClient.RunCommand(new Exists(remotePath));
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            return kerberosSftpClient.ExistsAsync(remotePath).GetAwaiter().GetResult();
        return _sftpClient.Exists(remotePath);
    }

    internal void DownloadFile(string remotePath, MemoryStream stream)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DownloadFile(remotePath, stream));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.DownloadFileAsync(remotePath, stream).GetAwaiter().GetResult();
        else
            _sftpClient.DownloadFile(remotePath, stream);
    }

    internal IEnumerable<SftpFile> ListDirectory(string hostTimeZone, string remotePath)
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
                items.AddRange(kerberosSftpClient.ListDirectoryAsync(hostTimeZone, remotePath).GetAwaiter().GetResult());
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
                    var resultItems = sftpClient.ListDirectory(remotePath);
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
                var resultItems = _sftpClient.ListDirectory(remotePath);
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

    internal void Delete(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath= remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.DeleteFileAsync(remotePath).GetAwaiter().GetResult();
        else
            _sftpClient.Delete(remotePath);
    }

    internal void DeleteFile(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.DeleteFileAsync(remotePath).GetAwaiter().GetResult();
        else
            _sftpClient.DeleteFile(remotePath);
    }

    internal void DeleteDirectory(string remotePath)
    {
        if (remotePath.StartsWith("~/"))
        {
            remotePath = remotePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            noAuthenticationSftpClient.RunCommand(new DeleteDirectory(remotePath));
        else if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.DeleteDirectoryAsync(remotePath).GetAwaiter().GetResult();
        else
            _sftpClient.DeleteDirectory(remotePath);
    }

    internal void DownloadFile(string fullName, FileStream targetStream)
    {
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "download file");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.DownloadFileAsync(fullName, targetStream).GetAwaiter().GetResult();
        else
            _sftpClient.DownloadFile(fullName, targetStream);
    }

    internal void CreateDirectory(string targetPath)
    {
        if (targetPath.StartsWith("~/"))
        {
            targetPath = targetPath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "create directory");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.CreateDirectoryAsync(targetPath).GetAwaiter().GetResult();
        else
            _sftpClient.CreateDirectory(targetPath);
    }

    internal void UploadFile(Stream sourceStream, string targetFilePath, bool canOverride)
    {
        if (targetFilePath.StartsWith("~/"))
        {
            targetFilePath = targetFilePath.Replace("~", WorkingDirectory);
        }
        if (_sftpClient is NoAuthenticationSftpClient)
            throw new SftpClientException("NoAuthenticationSftpClientMethod", "upload file");
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.UploadFileAsync(sourceStream, targetFilePath, canOverride).GetAwaiter().GetResult();
        else
            _sftpClient.UploadFile(sourceStream, targetFilePath, canOverride);
    }

    internal void RenameFile(string oldPath, string newPath)
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
            kerberosSftpClient.RenameAsync(oldPath, newPath).GetAwaiter().GetResult();
        else
            _sftpClient.RenameFile(oldPath, newPath);
    }
    
    internal SftpFileAttributes GetFileAttributes(string path)
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            return kerberosSftpClient.GetAttributesAsync(path).GetAwaiter().GetResult();
        return _sftpClient.GetAttributes(path);
    }

    internal void SetFileAttributes(string path, SftpFileAttributes fileAttributes)
    {
        if (_sftpClient is KerberosSftpClient kerberosSftpClient)
            kerberosSftpClient.SetAttributesAsync(path, fileAttributes).GetAwaiter().GetResult();
        else
            _sftpClient.SetAttributes(path, fileAttributes);
    }

    internal Stream OpenRead(string path)
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
            return kerberosSftpClient.OpenReadAsync(path).GetAwaiter().GetResult();
        }

        return _sftpClient.OpenRead(path);
    }

    #endregion
}
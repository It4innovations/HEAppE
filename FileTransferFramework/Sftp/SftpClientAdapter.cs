using HEAppE.FileTransferFramework.Sftp.Commands;
using HEAppE.Utils;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace HEAppE.FileTransferFramework.Sftp
{
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
        #region Methods
        internal void Connect()
        {
            if (_sftpClient is not NoAuthenticationSftpClient)
            {
                _sftpClient.Connect();
            }
        }

        internal void Disconnect()
        {
            if (_sftpClient is not NoAuthenticationSftpClient)
            {
                _sftpClient.Disconnect();
            }
        }

        internal bool Exists(string remotePath)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                return noAuthenticationSftpClient.RunCommand(new Exists(remotePath));
            }
            else
            {
                return _sftpClient.Exists(remotePath);
            }
        }

        internal void DownloadFile(string remotePath, MemoryStream stream)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                noAuthenticationSftpClient.RunCommand(new DownloadFile(remotePath, stream));
            }
            else
            {
                _sftpClient.DownloadFile(remotePath, stream);
            }
        }

        internal IEnumerable<SftpFile> ListDirectory(string remotePath)
        {
            var items = new List<SftpFile>();
            switch (_sftpClient)
            {
                case NoAuthenticationSftpClient noAuthenticationSftpClient:
                    string remoteWorkingDirectory = noAuthenticationSftpClient.RunCommand(new WorkingDirectory());
                    var result = noAuthenticationSftpClient.RunCommand(new ListDirectory(remotePath, remoteWorkingDirectory));
                    items.AddRange(result);
                    break;

                case ExtendedSftpClient extendedSftpClient:
                    {
                        try
                        {
                            var resultItems = extendedSftpClient.ListDirectory(remotePath);
                            foreach (var item in resultItems)
                            {
                                items.Add(new SftpFile
                                {
                                    FullName = item.FullName,
                                    IsDirectory = item.IsDirectory,
                                    IsSymbolicLink = item.IsSymbolicLink,
                                    LastWriteTime = item.LastWriteTime.Convert(extendedSftpClient.GetTimeZone()),
                                    Name = item.Name
                                });
                            }
                        }
                        catch (Renci.SshNet.Common.SshException exception)
                        {
                            //if directory is empty 'No such file' exception is raised, handle that case
                            switch(exception.Message)
                            {
                                case "No such file":
                                    break;
                                default:
                                    throw;
                            }

                        }

                    }break;

                default:
                    {
                        var resultItems = _sftpClient.ListDirectory(remotePath);
                        foreach (var item in resultItems)
                        {
                            items.Add(new SftpFile
                            {
                                FullName = item.FullName,
                                IsDirectory = item.IsDirectory,
                                IsSymbolicLink = item.IsSymbolicLink,
                                LastWriteTime = item.LastWriteTimeUtc,
                                Name = item.Name
                            });
                        }
                    }break;
            }
            return items;
        }

        internal void Delete(string remotePath)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
            }
            else
            {
                _sftpClient.Delete(remotePath);
            }
        }

        internal void DeleteFile(string remotePath)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                noAuthenticationSftpClient.RunCommand(new DeleteFile(remotePath));
            }
            else
            {
                _sftpClient.DeleteFile(remotePath);
            }
        }

        internal void DeleteDirectory(string remotePath)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                noAuthenticationSftpClient.RunCommand(new DeleteDirectory(remotePath));
            }
            else
            {
                _sftpClient.DeleteDirectory(remotePath);
            }
        }

        internal void DownloadFile(string fullName, FileStream targetStream)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                throw new Exception("NoAuthenticationSftpClient has not implemented download file method!");
            }
            else
            {
                _sftpClient.DownloadFile(fullName, targetStream);
            }
        }

        internal void CreateDirectory(string targetPath)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                throw new Exception("NoAuthenticationSftpClient has not implemented created directory method!");
            }
            else
            {
                _sftpClient.CreateDirectory(targetPath);
            }
        }

        internal void UploadFile(FileStream sourceStream, string targetFilePath, bool v)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                throw new Exception("NoAuthenticationSftpClient has not implemented upload file method!");
            }
            else
            {
                _sftpClient.UploadFile(sourceStream, targetFilePath, v);
            }
        }

        internal Stream OpenRead(string path)
        {
            if (_sftpClient is NoAuthenticationSftpClient noAuthenticationSftpClient)
            {
                MemoryStream ms = new MemoryStream();
                noAuthenticationSftpClient.RunCommand(new DownloadFile(path, ms));
                return ms;
            }
            else
            {
                return _sftpClient.OpenRead(path);
            }
        }
        #endregion
    }
}

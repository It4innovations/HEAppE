
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class SftpFileSystemManager : AbstractFileSystemManager
    {
        #region Instances
        private readonly IConnectionPool _connectionPool;
        #endregion
        #region Constructors
        public SftpFileSystemManager(ILogger logger, FileTransferMethod configuration, FileSystemFactory synchronizerFactory, IConnectionPool connectionPool)
            : base(logger, configuration, synchronizerFactory)
        {
            _connectionPool = connectionPool;
        }
        #endregion
        #region AbstractFileSystemManager Members
        public override byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath)
        {
            string jobClusterDirectoryPath = FileSystemUtils.GetJobClusterDirectoryPath(_fileSystem.Cluster.LocalBasepath, jobInfo.Specification);
            var connection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                using (var stream = new MemoryStream())
                {
                    string file = jobClusterDirectoryPath + relativeFilePath;
                    client.DownloadFile(file, stream);
                    return stream.ToArray();
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }
        public override byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification, string absoluteFilePath)
        {
            var connection = _connectionPool.GetConnectionForUser(jobSpecification.ClusterUser);
            try
            {
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                using var stream = new MemoryStream();
                var path = absoluteFilePath.Replace("~/", string.Empty).Replace("/~/", string.Empty);
                client.DownloadFile(path, stream);
                return stream.ToArray();
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }
        public override void DeleteSessionFromCluster(SubmittedJobInfo jobInfo)
        {
            string jobClusterDirectoryPath = FileSystemUtils.GetJobClusterDirectoryPath(_fileSystem.Cluster.LocalBasepath, jobInfo.Specification);
            var connection = _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser);
            try
            {
                string remotePath = jobClusterDirectoryPath;
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                DeleteRemoteDirectory(jobInfo.Specification.Cluster.TimeZone, remotePath, client);
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }
        protected override void CopyAll(string hostTimeZone, string source, string target, bool overwrite, DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials)
        {
            var connection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                if (Uri.IsWellFormedUriString(target, UriKind.Absolute))
                {
                    CopyAllToSftp(source, target, overwrite, lastModificationLimit, client, excludedFiles);
                }
                else
                {
                    if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
                    {
                        CopyAllFromSftp(hostTimeZone, source, target, overwrite, lastModificationLimit, client, excludedFiles);
                    }
                    else
                    {
                        FileSystemUtils.CopyAll(source, target, overwrite, lastModificationLimit, excludedFiles);
                    }
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }
        protected override ICollection<FileInformation> ListChangedFilesForTask(string hostTimeZone, string taskClusterDirectoryPath, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials)
        {
            var connection = _connectionPool.GetConnectionForUser(credentials);
            try
            {
                var client = new SftpClientAdapter((SftpClient)connection.Connection);
                return ListChangedFilesInDirectory(hostTimeZone, taskClusterDirectoryPath, taskClusterDirectoryPath, lastModificationLimit, credentials, client);
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }
        protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo, ClusterAuthenticationCredentials credentials)
        {
            SftpFullNameSynchronizer synchronizer = (SftpFullNameSynchronizer)_synchronizerFactory.CreateFileSynchronizer(fileInfo, credentials);
            synchronizer.ConnectionPool = _connectionPool;
            return synchronizer;
        }
        #endregion
        #region Local Methods
        private void DeleteRemoteDirectory(string hostTimeZone, string remotePath, SftpClientAdapter client)
        {
            _logger.LogDebug($"Starting delete remote directory {remotePath}");
            if (client.Exists(remotePath))
            {
                foreach (SftpFile file in client.ListDirectory(hostTimeZone, remotePath))
                {
                    if (file.Name == "." || file.Name == "..")
                    {
                        continue;
                    }

                    if (file.IsSymbolicLink)
                    {
                        _logger.LogDebug($"Deleting symlink {file.Name}");
                        client.Delete(file.FullName);
                    }
                    else
                    {
                        if (file.IsDirectory)
                        {
                            _logger.LogDebug($"Deleting subdirectory {file.Name}");
                            DeleteRemoteDirectory(hostTimeZone, file.FullName, client);
                        }
                        else
                        {
                            _logger.LogDebug($"Deleting file {file.Name}");
                            client.DeleteFile(file.FullName);
                        }
                    }
                }

                _logger.LogDebug($"Deleting root directory {remotePath}");
                client.DeleteDirectory(remotePath);
            }
        }

        private void CopyAllFromSftp(string hostTimeZone, string source, string target, bool overwrite, DateTime? lastModificationLimit, SftpClientAdapter client, string[] excludedFiles)
        {
            string sourcePath = source;
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            foreach (SftpFile file in client.ListDirectory(hostTimeZone, sourcePath))
            {
                if (file.Name == "." || file.Name == "..")
                {
                    continue;
                }

                if (file.IsDirectory)
                {
                    CopyAllFromSftp(hostTimeZone, FileSystemUtils.ConcatenatePaths(source, file.Name), Path.Combine(target, file.Name), overwrite,
                        lastModificationLimit, client, FileSystemUtils.GetExcludedFilesForSubdirectory(excludedFiles, file.Name));
                }
                else
                {
                    if (excludedFiles != null && excludedFiles.Contains(file.Name))
                    {
                        continue;
                    }

                    string targetFilePath = Path.Combine(target, file.Name);
                    if (((!File.Exists(targetFilePath)) || overwrite)
                            && ((!lastModificationLimit.HasValue) || (lastModificationLimit.Value < file.LastWriteTime)))
                    {
                        using FileStream targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        client.DownloadFile(file.FullName, targetStream);
                    }
                }
            }
        }

        private void CopyAllToSftp(string source, string target, bool overwrite, DateTime? lastModificationLimit, SftpClientAdapter client, string[] excludedFiles)
        {
            string targetPath = target;
            if (!client.Exists(targetPath))
            {
                client.CreateDirectory(targetPath);
            }

            DirectoryInfo sourceDir = new DirectoryInfo(source);
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                if (excludedFiles != null && excludedFiles.Contains(file.Name))
                {
                    continue;
                }

                string targetFilePath = FileSystemUtils.ConcatenatePaths(targetPath, file.Name);
                if (((!client.Exists(targetFilePath)) || overwrite) && ((!lastModificationLimit.HasValue) || (lastModificationLimit.Value < file.LastWriteTime)))
                {
                    using FileStream sourceStream = file.OpenRead();
                    client.UploadFile(sourceStream, targetFilePath, true);
                }
            }
            foreach (DirectoryInfo directory in sourceDir.GetDirectories())
            {
                CopyAllToSftp(directory.FullName, FileSystemUtils.ConcatenatePaths(target, directory.Name), overwrite, lastModificationLimit, client, FileSystemUtils.GetExcludedFilesForSubdirectory(excludedFiles, directory.Name));
            }
        }

        private ICollection<FileInformation> ListChangedFilesInDirectory(string hostTimeZone, string rootDirectory, string currentDirectory, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials, SftpClientAdapter client)
        {
            List<FileInformation> results = new List<FileInformation>();
            foreach (SftpFile file in client.ListDirectory(hostTimeZone, currentDirectory))
            {
                if (file.Name == "." || file.Name == "..")
                {
                    continue;
                }

                if (file.IsDirectory)
                {
                    results.AddRange(ListChangedFilesInDirectory(hostTimeZone, rootDirectory, FileSystemUtils.ConcatenatePaths(currentDirectory, file.Name), lastModificationLimit, credentials, client));
                }
                else if (((!lastModificationLimit.HasValue) || (lastModificationLimit.Value <= file.LastWriteTime)))
                {
                    results.Add(new FileInformation
                    {
                        FileName = file.FullName.Replace(rootDirectory, string.Empty),
                        LastModifiedDate = file.LastWriteTime
                    });
                }
            }
            return results;
        }
        #endregion
    }
}
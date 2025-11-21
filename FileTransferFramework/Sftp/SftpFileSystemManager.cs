using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using log4net;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;

namespace HEAppE.FileTransferFramework.Sftp;

public class SftpFileSystemManager : AbstractFileSystemManager
{
    #region Constructors

    public SftpFileSystemManager(ILogger logger, FileTransferMethod configuration,
        FileSystemFactory synchronizerFactory, IConnectionPool connectionPool)
        : base(logger, configuration, synchronizerFactory)
    {
        _connectionPool = connectionPool;
    }

    #endregion

    #region Instances

    private readonly IConnectionPool _connectionPool;

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected new readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    #endregion

    #region AbstractFileSystemManager Members

    public override byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken)
    {
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        var localBasePath = Path.Combine(basePath, _scripts.SubExecutionsPath.TrimStart('/'));

        var partPath = localBasePath.Replace(basePath, string.Empty);

        var connection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken);
        _logger.LogInformation($"Downloading file {relativeFilePath} from cluster");
        try
        {
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            using (var stream = new MemoryStream())
            {
                if(basePath.StartsWith("~"))
                    basePath = basePath.Replace("~", ((SftpClient)connection.Connection).WorkingDirectory);
                
                var file = Path.Combine(basePath, _scripts.InstanceIdentifierPath, partPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username, relativeFilePath.TrimStart('/'));
                client.DownloadFile(file, stream);
                return stream.ToArray();
            }
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }
    }

    public override byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification,
        string absoluteFilePath, string sshCaToken)
    {
        _logger.LogInformation($"Downloading file {absoluteFilePath} from cluster");
        var connection = _connectionPool.GetConnectionForUser(jobSpecification.ClusterUser, jobSpecification.Cluster, sshCaToken);
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

    public override void DeleteSessionFromCluster(SubmittedJobInfo jobInfo, string sshCaToken)
    {
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath);
        var connection =
            _connectionPool.GetConnectionForUser(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken);
        try
        {
            var remotePath = jobClusterDirectoryPath;
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            DeleteRemoteDirectory(jobInfo.Specification.Cluster.TimeZone, remotePath, client);
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }
    }

    protected override void CopyAll(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken)
    {
        var connection = _connectionPool.GetConnectionForUser(credentials, cluster, sshCaToken);
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
                    CopyAllFromSftp(hostTimeZone, source, target, overwrite, lastModificationLimit, client,
                        excludedFiles);
                else
                    FileSystemUtils.CopyAll(source, target, overwrite, lastModificationLimit, excludedFiles);
            }
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }
    }

    protected override ICollection<FileInformation> ListChangedFilesForTask(string hostTimeZone,
        string taskClusterDirectoryPath, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken)
    {
        var connection = _connectionPool.GetConnectionForUser(credentials, cluster, sshCaToken);
        try
        {
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            return ListChangedFilesInDirectory(hostTimeZone, taskClusterDirectoryPath, taskClusterDirectoryPath,
                lastModificationLimit, credentials, client);
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }
    }

    protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken)
    {
        var synchronizer = (SftpFullNameSynchronizer)_synchronizerFactory.CreateFileSynchronizer(fileInfo, credentials);
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
            foreach (var file in client.ListDirectory(hostTimeZone, remotePath))
            {
                if (file.Name == "." || file.Name == "..") continue;

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

    private void CopyAllFromSftp(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, SftpClientAdapter client, string[] excludedFiles)
    {
        var sourcePath = source;
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var file in client.ListDirectory(hostTimeZone, sourcePath))
        {
            if (file.Name == "." || file.Name == "..") continue;

            if (file.IsDirectory)
            {
                CopyAllFromSftp(hostTimeZone, FileSystemUtils.ConcatenatePaths(source, file.Name),
                    Path.Combine(target, file.Name), overwrite,
                    lastModificationLimit, client,
                    FileSystemUtils.GetExcludedFilesForSubdirectory(excludedFiles, file.Name));
            }
            else
            {
                if (excludedFiles != null && excludedFiles.Contains(file.Name)) continue;

                var targetFilePath = Path.Combine(target, file.Name);
                if ((!File.Exists(targetFilePath) || overwrite)
                    && (!lastModificationLimit.HasValue || lastModificationLimit.Value < file.LastWriteTime))
                {
                    using var targetStream =
                        new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    client.DownloadFile(file.FullName, targetStream);
                }
            }
        }
    }

    private void CopyAllToSftp(string source, string target, bool overwrite, DateTime? lastModificationLimit,
        SftpClientAdapter client, string[] excludedFiles)
    {
        var targetPath = target;
        if (!client.Exists(targetPath)) client.CreateDirectory(targetPath);

        var sourceDir = new DirectoryInfo(source);
        foreach (var file in sourceDir.GetFiles())
        {
            if (excludedFiles != null && excludedFiles.Contains(file.Name)) continue;

            var targetFilePath = FileSystemUtils.ConcatenatePaths(targetPath, file.Name);
            if ((!client.Exists(targetFilePath) || overwrite) &&
                (!lastModificationLimit.HasValue || lastModificationLimit.Value < file.LastWriteTime))
            {
                using var sourceStream = file.OpenRead();
                client.UploadFile(sourceStream, targetFilePath, true);
            }
        }

        foreach (var directory in sourceDir.GetDirectories())
            CopyAllToSftp(directory.FullName, FileSystemUtils.ConcatenatePaths(target, directory.Name), overwrite,
                lastModificationLimit, client,
                FileSystemUtils.GetExcludedFilesForSubdirectory(excludedFiles, directory.Name));
    }

    private ICollection<FileInformation> ListChangedFilesInDirectory(string hostTimeZone, string rootDirectory,
        string currentDirectory, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials,
        SftpClientAdapter client)
    {
        var results = new List<FileInformation>();
        foreach (var file in client.ListDirectory(hostTimeZone, currentDirectory))
        {
            if (file.Name == "." || file.Name == "..") continue;

            if (file.IsDirectory)
                results.AddRange(ListChangedFilesInDirectory(hostTimeZone, rootDirectory,
                    FileSystemUtils.ConcatenatePaths(currentDirectory, file.Name), lastModificationLimit, credentials,
                    client));
            else if (!lastModificationLimit.HasValue || lastModificationLimit.Value <= file.LastWriteTime)
                results.Add(new FileInformation
                {
                    FileName = file.FullName.Replace(rootDirectory, string.Empty),
                    LastModifiedDate = file.LastWriteTime
                });
        }

        return results;
    }

    public override bool UploadFileToClusterByAbsolutePath(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken)
    {
        bool result = false;
        var connection = _connectionPool.GetConnectionForUser(credentials, cluster, sshCaToken);
        try
        {
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            client.UploadFile(fileStream, absoluteFilePath.Replace('\\', '/'), true);
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }

        return result;
    }
    
    public override bool ModifyAbsolutePathFileAttributes(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken,
        bool? ownerCanExecute = null, bool? groupCanExecute = null)
    {
        bool result = false;
        absoluteFilePath = absoluteFilePath.Replace('\\', '/');
        var connection = _connectionPool.GetConnectionForUser(credentials, cluster, sshCaToken);
        try
        {
            var client = new SftpClientAdapter((SftpClient)connection.Connection);
            
            var fileAttributes = client.GetFileAttributes(absoluteFilePath);
            if (ownerCanExecute.HasValue)
                fileAttributes.OwnerCanExecute = ownerCanExecute.Value;
            if (groupCanExecute.HasValue)
                fileAttributes.GroupCanExecute = groupCanExecute.Value;
            client.SetFileAttributes(absoluteFilePath, fileAttributes);
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            _connectionPool.ReturnConnection(connection);
        }

        return result;
    }

    #endregion
}
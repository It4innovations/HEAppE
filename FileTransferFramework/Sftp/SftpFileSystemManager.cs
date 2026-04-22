using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public override async Task<byte[]> DownloadFileFromClusterAsync(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken, string lexisToken)
    {
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        
        var localBasePath = Path.Combine(basePath, _scripts.SubExecutionsPath.TrimStart('/'));

        var partPath = localBasePath.Replace(basePath, string.Empty);

        var connection =
            await _connectionPool.GetConnectionForUserAsync(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        _logger.LogInformation($"Downloading file {relativeFilePath} from cluster");
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            using (var stream = new MemoryStream())
            {
                if(basePath.StartsWith("~"))
                    basePath = basePath.Replace("~", client.WorkingDirectory);
                
                var file = Path.Combine(basePath, _scripts.InstanceIdentifierPath, partPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username, relativeFilePath.TrimStart('/'));
                await client.DownloadFileAsync(file, stream);
                return stream.ToArray();
            }
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }
    }

    public override async Task<byte[]> DownloadFileFromClusterByAbsolutePathAsync(JobSpecification jobSpecification,
        string absoluteFilePath, string sshCaToken, string lexisToken)
    {
        _logger.LogInformation($"Downloading file {absoluteFilePath} from cluster");
        var connection = await _connectionPool.GetConnectionForUserAsync(jobSpecification.ClusterUser, jobSpecification.Cluster, sshCaToken, lexisToken);
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            using var stream = new MemoryStream();
            var path = absoluteFilePath.Replace("~/", string.Empty).Replace("/~/", string.Empty);
            await client.DownloadFileAsync(path, stream);
            return stream.ToArray();
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }
    }

    public override async Task DeleteSessionFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath);
        var connection =
            await _connectionPool.GetConnectionForUserAsync(jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        try
        {
            var remotePath = jobClusterDirectoryPath;
            var client = SftpClientAdapter.FromObject(connection.Connection);
            await DeleteRemoteDirectoryAsync(jobInfo.Specification.Cluster.TimeZone, remotePath, client);
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }
    }

    protected override async Task CopyAllAsync(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken, string lexisToken)
    {
        var connection = await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            if (Uri.IsWellFormedUriString(target, UriKind.Absolute))
            {
                await CopyAllToSftpAsync(source, target, overwrite, lastModificationLimit, client, excludedFiles);
            }
            else
            {
                if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
                    await CopyAllFromSftpAsync(hostTimeZone, source, target, overwrite, lastModificationLimit, client,
                        excludedFiles);
                else
                    FileSystemUtils.CopyAll(source, target, overwrite, lastModificationLimit, excludedFiles);
            }
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }
    }

    protected override async Task<ICollection<FileInformation>> ListChangedFilesForTaskAsync(string hostTimeZone,
        string taskClusterDirectoryPath, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken, string lexisToken)
    {
        var connection = await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            return await ListChangedFilesInDirectoryAsync(hostTimeZone, taskClusterDirectoryPath, taskClusterDirectoryPath,
                lastModificationLimit, credentials, client);
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }
    }

    protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        var synchronizer = (SftpFullNameSynchronizer)_synchronizerFactory.CreateFileSynchronizer(fileInfo, credentials);
        synchronizer.ConnectionPool = _connectionPool;
        return synchronizer;
    }

    #endregion

    #region Local Methods

    private async Task DeleteRemoteDirectoryAsync(string hostTimeZone, string remotePath, SftpClientAdapter client)
    {
        _logger.LogDebug($"Starting delete remote directory {remotePath}");
        if (await client.ExistsAsync(remotePath))
        {
            foreach (var file in await client.ListDirectoryAsync(hostTimeZone, remotePath))
            {
                if (file.Name == "." || file.Name == "..") continue;

                if (file.IsSymbolicLink)
                {
                    _logger.LogDebug($"Deleting symlink {file.Name}");
                    await client.DeleteAsync(file.FullName);
                }
                else
                {
                    if (file.IsDirectory)
                    {
                        _logger.LogDebug($"Deleting subdirectory {file.Name}");
                        await DeleteRemoteDirectoryAsync(hostTimeZone, file.FullName, client);
                    }
                    else
                    {
                        _logger.LogDebug($"Deleting file {file.Name}");
                        await client.DeleteFileAsync(file.FullName);
                    }
                }
            }

            _logger.LogDebug($"Deleting root directory {remotePath}");
            await client.DeleteDirectoryAsync(remotePath);
        }
    }

    private async Task CopyAllFromSftpAsync(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, SftpClientAdapter client, string[] excludedFiles)
    {
        var sourcePath = source;
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var file in await client.ListDirectoryAsync(hostTimeZone, sourcePath))
        {
            if (file.Name == "." || file.Name == "..") continue;

            if (file.IsDirectory)
            {
                await CopyAllFromSftpAsync(hostTimeZone, FileSystemUtils.ConcatenatePaths(source, file.Name),
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
                    await client.DownloadFileAsync(file.FullName, targetStream);
                }
            }
        }
    }

    private async Task CopyAllToSftpAsync(string source, string target, bool overwrite, DateTime? lastModificationLimit,
        SftpClientAdapter client, string[] excludedFiles)
    {
        var targetPath = target;
        if (!await client.ExistsAsync(targetPath)) await client.CreateDirectoryAsync(targetPath);

        var sourceDir = new DirectoryInfo(source);
        foreach (var file in sourceDir.GetFiles())
        {
            if (excludedFiles != null && excludedFiles.Contains(file.Name)) continue;

            var targetFilePath = FileSystemUtils.ConcatenatePaths(targetPath, file.Name);
            if ((!await client.ExistsAsync(targetFilePath) || overwrite) &&
                (!lastModificationLimit.HasValue || lastModificationLimit.Value < file.LastWriteTime))
            {
                using var sourceStream = file.OpenRead();
                await client.UploadFileAsync(sourceStream, targetFilePath, true);
            }
        }

        foreach (var directory in sourceDir.GetDirectories())
            await CopyAllToSftpAsync(directory.FullName, FileSystemUtils.ConcatenatePaths(target, directory.Name), overwrite,
                lastModificationLimit, client,
                FileSystemUtils.GetExcludedFilesForSubdirectory(excludedFiles, directory.Name));
    }

    private async Task<ICollection<FileInformation>> ListChangedFilesInDirectoryAsync(string hostTimeZone, string rootDirectory,
        string currentDirectory, DateTime? lastModificationLimit, ClusterAuthenticationCredentials credentials,
        SftpClientAdapter client)
    {
        var results = new List<FileInformation>();
        foreach (var file in await client.ListDirectoryAsync(hostTimeZone, currentDirectory))
        {
            if (file.Name == "." || file.Name == "..") continue;
            
            var fullPath = FileSystemUtils.ConcatenatePaths(currentDirectory, file.Name);

            if (file.IsDirectory)
            {
                results.AddRange(await ListChangedFilesInDirectoryAsync(hostTimeZone, rootDirectory,
                    fullPath, lastModificationLimit, credentials, client));
            }
            else if (!lastModificationLimit.HasValue || lastModificationLimit.Value <= file.LastWriteTime)
            {
                string relativeFileName;
                int index = fullPath.IndexOf(rootDirectory, StringComparison.Ordinal);

                if (index != -1)
                {
                    relativeFileName = fullPath.Substring(index + rootDirectory.Length).TrimStart('/');
                }
                else
                {
                    relativeFileName = file.Name;
                }

                results.Add(new FileInformation
                {
                    FileName = relativeFileName,
                    LastModifiedDate = file.LastWriteTime
                });
            }
        }

        return results;
    }

    public override async Task<bool> UploadFileToClusterByAbsolutePathAsync(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        bool result = false;
        var connection = await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            var sftpClient = (SftpClient)connection.Connection;
            var client = new SftpClientAdapter(sftpClient);
            absoluteFilePath = absoluteFilePath.Replace('\\', '/');
            if (absoluteFilePath.StartsWith("~/"))
            {
                absoluteFilePath = absoluteFilePath.Replace("~", client.WorkingDirectory);
            }
            try
            {
                await client.UploadFileAsync(fileStream, absoluteFilePath + ".part", true);
                try {
                    if (await client.ExistsAsync(absoluteFilePath))
                        await client.DeleteFileAsync(absoluteFilePath);
                } catch {
                }
                await client.RenameFileAsync(absoluteFilePath + ".part", absoluteFilePath);
            }
            catch (Exception )
            {
                try {
                    if (await client.ExistsAsync(absoluteFilePath + ".part"))
                        await client.DeleteFileAsync(absoluteFilePath + ".part");
                } catch {
                }
                throw;
            }
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }

        return result;
    }
    
    public override async Task<bool> ModifyAbsolutePathFileAttributesAsync(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken,
        bool? ownerCanExecute = null, bool? groupCanExecute = null)
    {
        bool result = false;
        absoluteFilePath = absoluteFilePath.Replace('\\', '/');
        var connection = await _connectionPool.GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken);
        try
        {
            var client = SftpClientAdapter.FromObject(connection.Connection);
            
            var fileAttributes = await client.GetFileAttributesAsync(absoluteFilePath);
            if (ownerCanExecute.HasValue)
                fileAttributes.OwnerCanExecute = ownerCanExecute.Value;
            if (groupCanExecute.HasValue)
                fileAttributes.GroupCanExecute = groupCanExecute.Value;
            await client.SetFileAttributesAsync(absoluteFilePath, fileAttributes);
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            await _connectionPool.ReturnConnectionAsync(connection);
        }

        return result;
    }

    #endregion
}
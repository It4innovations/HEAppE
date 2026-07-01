using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;

namespace HEAppE.FileTransferFramework;

public abstract class AbstractFileSystemManager : IRexFileSystemManager
{
    #region Constructors

    public AbstractFileSystemManager(ILogger logger, FileTransferMethod configuration,
        FileSystemFactory synchronizerFactory)
    {
        _logger = logger;
        _fileSystem = configuration;
        _synchronizerFactory = synchronizerFactory;
        _fileSynchronizers = new Dictionary<SynchronizableFiles, Dictionary<string, IFileSynchronizer>>();
    }

    #endregion

    #region Instances

    protected ILogger _logger;
    protected Dictionary<SynchronizableFiles, Dictionary<string, IFileSynchronizer>> _fileSynchronizers;

    protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;
    protected FileTransferMethod _fileSystem;
    protected FileSystemFactory _synchronizerFactory;

    #endregion

    #region Abstract Methods

    public abstract Task<byte[]> DownloadFileFromClusterAsync(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken, string lexisToken);

    public abstract Task<byte[]> DownloadFileFromClusterByAbsolutePathAsync(JobSpecification jobSpecification,
        string absoluteFilePath, string sshCaToken, string lexisToken);

    public abstract Task DeleteSessionFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken);

    protected abstract Task CopyAllAsync(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken, string lexisToken);

    protected abstract Task<ICollection<FileInformation>> ListChangedFilesForTaskAsync(string hostTimeZone,
        string taskClusterDirectoryPath, DateTime? jobSubmitTime,
        ClusterAuthenticationCredentials clusterAuthenticationCredentials, Cluster cluster, string sshCaToken, string lexisToken);

    protected abstract IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo,
        ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken);

    public abstract Task<bool> UploadFileToClusterByAbsolutePathAsync(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken);
    
    public abstract Task<bool> ModifyAbsolutePathFileAttributesAsync(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken,
        bool? ownerCanExecute = null, bool? groupCanExecute = null);
    #endregion

    #region IRexFileSystemManager Members

    public virtual async Task CopyInputFilesToClusterAsync(SubmittedJobInfo jobInfo, string localJobDirectory, string sshCaToken, string lexisToken)
    {
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath);
        await CopyAllAsync(jobInfo.Specification.Cluster.TimeZone, localJobDirectory, jobClusterDirectoryPath, false, null, null,
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster, sshCaToken, lexisToken);
    }

    public virtual Task<ICollection<JobFileContent>> CopyStdOutputFilesFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        return PerformSynchronizationForTypeAsync(jobInfo, SynchronizableFiles.StandardOutputFile, sshCaToken, lexisToken);
    }

    public virtual Task<ICollection<JobFileContent>> CopyStdErrorFilesFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        return PerformSynchronizationForTypeAsync(jobInfo, SynchronizableFiles.StandardErrorFile, sshCaToken, lexisToken);
    }

    public virtual Task<ICollection<JobFileContent>> CopyProgressFilesFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        return PerformSynchronizationForTypeAsync(jobInfo, SynchronizableFiles.ProgressFile, sshCaToken, lexisToken);
    }

    public virtual Task<ICollection<JobFileContent>> CopyLogFilesFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        return PerformSynchronizationForTypeAsync(jobInfo, SynchronizableFiles.LogFile, sshCaToken, lexisToken);
    }

    public virtual async Task<ICollection<JobFileContent>> DownloadPartOfJobFileFromClusterAsync(SubmittedTaskInfo taskInfo,
        SynchronizableFiles fileType, long offset, string instancePath, string subPath, string sshCaToken, string lexisToken)
    {
        var taskClusterDirectoryPath = string.Empty;
        if (taskInfo.State == TaskState.Deleted)
        {
            taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterArchiveDirectoryPath(taskInfo.Specification, instancePath, subPath);
        }
        else
        {
            taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, instancePath, subPath);
        }
        var fileInfo = CreateSynchronizableFileInfoForType(taskInfo.Specification, taskClusterDirectoryPath, fileType);
        var synchronizer = CreateFileSynchronizer(fileInfo, taskInfo.Specification.JobSpecification.ClusterUser, sshCaToken, lexisToken);
        synchronizer.Offset = offset;
        synchronizer.SyncFileInfo.DestinationDirectory = null;
        var jobSpecification = taskInfo.Specification.JobSpecification;
        var result = await synchronizer.SynchronizeFilesAsync(jobSpecification.Cluster, sshCaToken, lexisToken);

        if (result != null)
            foreach (var content in result)
            {
                content.FileType = fileType;
                content.SubmittedTaskInfoId = taskInfo.Id;
            }

        return result;
    }

    public virtual async Task CopyCreatedFilesFromClusterAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken)
    {
        foreach (var taskInfo in jobInfo.Tasks)
        {
            var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath);

            string[] excludedFiles =
            {
                taskInfo.Specification.LogFile.RelativePath,
                taskInfo.Specification.ProgressFile.RelativePath,
                taskInfo.Specification.StandardOutputFile,
                taskInfo.Specification.StandardErrorFile
            };
            await CopyAllAsync(jobInfo.Specification.Cluster.TimeZone, taskClusterDirectoryPath,
                taskInfo.Specification.LocalDirectory, true, jobSubmitTime, excludedFiles,
                jobInfo.Specification.ClusterUser,
                jobInfo.Specification.Cluster, sshCaToken, lexisToken);
        }
    }

    public virtual async Task<ICollection<FileInformation>> ListFilesForJobAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string instancePath, string subPath, string sshCaToken, string lexisToken)
    {
        var result = new List<FileInformation>();

        foreach (var taskInfo in jobInfo.Tasks)
        {
            string taskClusterDirectoryPath = jobInfo.State == JobState.Deleted ? 
                FileSystemUtils.GetTaskClusterArchiveDirectoryPath(taskInfo.Specification, instancePath, subPath) : 
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, instancePath, subPath);
            
            _logger.LogInformation("Listing files in {0} for task {1}", taskClusterDirectoryPath, taskInfo.Specification.Id);

            var changedFiles = await ListChangedFilesForTaskAsync(
                jobInfo.Specification.Cluster.TimeZone, 
                taskClusterDirectoryPath,
                jobSubmitTime, 
                jobInfo.Specification.ClusterUser, 
                jobInfo.Specification.Cluster, sshCaToken, lexisToken);

            foreach (var changedFile in changedFiles)
            {
                var relativeFilePath = Path.Combine(
                    taskInfo.Specification.Id.ToString(CultureInfo.InvariantCulture)?.TrimStart('/'),
                    taskInfo.Specification.ClusterTaskSubdirectory?.TrimStart('/') ?? string.Empty,
                    changedFile.FileName.TrimStart('/'));

                result.Add(new FileInformation
                {
                    FileName = $"/{relativeFilePath}",
                    LastModifiedDate = changedFile.LastModifiedDate
                });
            }
        }

        return result;
    }

    public virtual Task<ICollection<FileInformation>> ListChangedFilesForJobAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken)
    {
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        return ListFilesForJobAsync(jobInfo, jobSubmitTime, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath, sshCaToken, lexisToken);
    }

    public virtual Task<ICollection<FileInformation>> ListArchivedFilesForJobAsync(SubmittedJobInfo jobInfo, DateTime jobSubmitTime, string sshCaToken, string lexisToken)
    {
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        return ListFilesForJobAsync(jobInfo, jobSubmitTime, clusterConfig.InstanceIdentifierPath, clusterConfig.JobLogArchiveSubPath, sshCaToken, lexisToken);
    }


    #endregion

    #region Local Methods

    protected virtual async Task<ICollection<JobFileContent>> PerformSynchronizationForTypeAsync(SubmittedJobInfo jobInfo,
        SynchronizableFiles fileType, string sshCaToken, string lexisToken)
    {
        var result = new List<JobFileContent>();
        if (!_fileSynchronizers.ContainsKey(fileType))
            _fileSynchronizers[fileType] = new Dictionary<string, IFileSynchronizer>(jobInfo.Tasks.Count);

        foreach (var taskInfo in jobInfo.Tasks)
        {
            var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath);
            var fileInfo =
                CreateSynchronizableFileInfoForType(taskInfo.Specification, taskClusterDirectoryPath, fileType);
            var sourceFilePath = FileSystemUtils.ConcatenatePaths(fileInfo.SourceDirectory, fileInfo.RelativePath);

            if (!_fileSynchronizers[fileType].ContainsKey(sourceFilePath))
                _fileSynchronizers[fileType][sourceFilePath] =
                    CreateFileSynchronizer(fileInfo, jobInfo.Specification.ClusterUser, sshCaToken, lexisToken);
            var jobSpecification = jobInfo.Specification;
            var subresult = await _fileSynchronizers[fileType][sourceFilePath].SynchronizeFilesAsync(jobSpecification.Cluster, sshCaToken, lexisToken);
            if (subresult != null)
                foreach (var content in subresult)
                {
                    content.FileType = fileType;
                    content.SubmittedTaskInfoId = taskInfo.Id;
                    result.Add(content);
                }
        }

        return result;
    }

    protected virtual void CreateSynchronizersForType(JobSpecification jobSpecification, SynchronizableFiles fileType, string sshCaToken, string lexisToken)
    {
        _fileSynchronizers[fileType] = new Dictionary<string, IFileSynchronizer>(jobSpecification.Tasks.Count);

        foreach (var task in jobSpecification.Tasks)
        {
            var clusterConfig = ClusterRuntimeConfiguration.For(jobSpecification.Cluster.CustomConfiguration);
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(task, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath);
            var fileInfo = CreateSynchronizableFileInfoForType(task, taskClusterDirectoryPath, fileType);
            var sourceFilePath = FileSystemUtils.ConcatenatePaths(fileInfo.SourceDirectory, fileInfo.RelativePath);

            if (!_fileSynchronizers[fileType].ContainsKey(sourceFilePath))
                _fileSynchronizers[fileType][sourceFilePath] =
                    CreateFileSynchronizer(fileInfo, jobSpecification.ClusterUser, sshCaToken, lexisToken);
        }
    }

    protected virtual FullFileSpecification CreateSynchronizableFileInfoForType(TaskSpecification task,
        string taskClusterDirectoryPath,
        SynchronizableFiles fileType)
    {
        var fileInfo = new FullFileSpecification
        {
            DestinationDirectory = task.LocalDirectory,
            SourceDirectory = taskClusterDirectoryPath
        };
        CompleteFileInfoForType(fileInfo, task, fileType);
        return fileInfo;
    }

    protected virtual void CompleteFileInfoForType(FileSpecification fileInfo, TaskSpecification task,
        SynchronizableFiles fileType)
    {
        switch (fileType)
        {
            case SynchronizableFiles.StandardOutputFile:
                fileInfo.RelativePath = task.StandardOutputFile;
                fileInfo.NameSpecification = FileNameSpecification.FullName;
                fileInfo.SynchronizationType = FileSynchronizationType.IncrementalAppend;
                break;
            case SynchronizableFiles.StandardErrorFile:
                fileInfo.RelativePath = task.StandardErrorFile;
                fileInfo.NameSpecification = FileNameSpecification.FullName;
                fileInfo.SynchronizationType = FileSynchronizationType.IncrementalAppend;
                break;
            case SynchronizableFiles.LogFile:
                fileInfo.RelativePath = task.LogFile.RelativePath;
                fileInfo.NameSpecification = task.LogFile.NameSpecification;
                fileInfo.SynchronizationType = task.LogFile.SynchronizationType;
                break;
            case SynchronizableFiles.ProgressFile:
                fileInfo.RelativePath = task.ProgressFile.RelativePath;
                fileInfo.NameSpecification = task.ProgressFile.NameSpecification;
                fileInfo.SynchronizationType = task.ProgressFile.SynchronizationType;
                break;
        }
    }

    protected virtual Task<ICollection<JobFileContent>> SynchronizeAllFilesOfTypeAsync(SynchronizableFiles fileType)
    {
        throw new NotImplementedException();
    }

    #endregion
}
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

    public abstract byte[] DownloadFileFromCluster(SubmittedJobInfo jobInfo, string relativeFilePath);

    public abstract byte[] DownloadFileFromClusterByAbsolutePath(JobSpecification jobSpecification,
        string absoluteFilePath);

    public abstract void DeleteSessionFromCluster(SubmittedJobInfo jobInfo);

    protected abstract void CopyAll(string hostTimeZone, string source, string target, bool overwrite,
        DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials,
        Cluster cluster);

    protected abstract ICollection<FileInformation> ListChangedFilesForTask(string hostTimeZone,
        string taskClusterDirectoryPath, DateTime? jobSubmitTime,
        ClusterAuthenticationCredentials clusterAuthenticationCredentials, Cluster cluster);

    protected abstract IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo,
        ClusterAuthenticationCredentials credentials);

    #endregion

    #region IRexFileSystemManager Members

    public virtual void CopyInputFilesToCluster(SubmittedJobInfo jobInfo, string localJobDirectory)
    {
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath);
        CopyAll(jobInfo.Specification.Cluster.TimeZone, localJobDirectory, jobClusterDirectoryPath, false, null, null,
            jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
    }

    public virtual ICollection<JobFileContent> CopyStdOutputFilesFromCluster(SubmittedJobInfo jobInfo)
    {
        return PerformSynchronizationForType(jobInfo, SynchronizableFiles.StandardOutputFile);
    }

    public virtual ICollection<JobFileContent> CopyStdErrorFilesFromCluster(SubmittedJobInfo jobInfo)
    {
        return PerformSynchronizationForType(jobInfo, SynchronizableFiles.StandardErrorFile);
    }

    public virtual ICollection<JobFileContent> CopyProgressFilesFromCluster(SubmittedJobInfo jobInfo)
    {
        return PerformSynchronizationForType(jobInfo, SynchronizableFiles.ProgressFile);
    }

    public virtual ICollection<JobFileContent> CopyLogFilesFromCluster(SubmittedJobInfo jobInfo)
    {
        return PerformSynchronizationForType(jobInfo, SynchronizableFiles.LogFile);
    }

    public virtual ICollection<JobFileContent> DownloadPartOfJobFileFromCluster(SubmittedTaskInfo taskInfo,
        SynchronizableFiles fileType, long offset)
    {
        var jobClusterDirectoryPath =
            FileSystemUtils.GetJobClusterDirectoryPath(taskInfo.Specification.JobSpecification,
                _scripts.SubExecutionsPath);
        var taskClusterDirectoryPath =
            FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, _scripts.SubExecutionsPath);
        var fileInfo = CreateSynchronizableFileInfoForType(taskInfo.Specification, taskClusterDirectoryPath, fileType);
        var synchronizer = CreateFileSynchronizer(fileInfo, taskInfo.Specification.JobSpecification.ClusterUser);
        synchronizer.Offset = offset;
        synchronizer.SyncFileInfo.DestinationDirectory = null;
        var jobSpecification = taskInfo.Specification.JobSpecification;
        var result = synchronizer.SynchronizeFiles(jobSpecification.Cluster);

        if (result != null)
            foreach (var content in result)
            {
                content.FileType = fileType;
                content.SubmittedTaskInfoId = taskInfo.Id;
            }

        return result;
    }

    public virtual void CopyCreatedFilesFromCluster(SubmittedJobInfo jobInfo, DateTime jobSubmitTime)
    {
        foreach (var taskInfo in jobInfo.Tasks)
        {
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, _scripts.SubExecutionsPath);

            string[] excludedFiles =
            {
                taskInfo.Specification.LogFile.RelativePath,
                taskInfo.Specification.ProgressFile.RelativePath,
                taskInfo.Specification.StandardOutputFile,
                taskInfo.Specification.StandardErrorFile
            };
            CopyAll(jobInfo.Specification.Cluster.TimeZone, taskClusterDirectoryPath,
                taskInfo.Specification.LocalDirectory, true, jobSubmitTime, excludedFiles,
                jobInfo.Specification.ClusterUser,
                jobInfo.Specification.Cluster);
        }
    }

    public virtual ICollection<FileInformation> ListChangedFilesForJob(SubmittedJobInfo jobInfo, DateTime jobSubmitTime)
    {
        var result = new List<FileInformation>();
        foreach (var taskInfo in jobInfo.Tasks)
        {
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, _scripts.SubExecutionsPath);

            var changedFiles = ListChangedFilesForTask(jobInfo.Specification.Cluster.TimeZone, taskClusterDirectoryPath,
                jobSubmitTime, jobInfo.Specification.ClusterUser, jobInfo.Specification.Cluster);
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

    #endregion

    #region Local Methods

    protected virtual ICollection<JobFileContent> PerformSynchronizationForType(SubmittedJobInfo jobInfo,
        SynchronizableFiles fileType)
    {
        var result = new List<JobFileContent>();
        if (!_fileSynchronizers.ContainsKey(fileType))
            _fileSynchronizers[fileType] = new Dictionary<string, IFileSynchronizer>(jobInfo.Tasks.Count);

        foreach (var taskInfo in jobInfo.Tasks)
        {
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(taskInfo.Specification, _scripts.SubExecutionsPath);
            var fileInfo =
                CreateSynchronizableFileInfoForType(taskInfo.Specification, taskClusterDirectoryPath, fileType);
            var sourceFilePath = FileSystemUtils.ConcatenatePaths(fileInfo.SourceDirectory, fileInfo.RelativePath);

            if (!_fileSynchronizers[fileType].ContainsKey(sourceFilePath))
                _fileSynchronizers[fileType][sourceFilePath] =
                    CreateFileSynchronizer(fileInfo, jobInfo.Specification.ClusterUser);
            var jobSpecification = jobInfo.Specification;
            var subresult = _fileSynchronizers[fileType][sourceFilePath].SynchronizeFiles(jobSpecification.Cluster);
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

    protected virtual void CreateSynchronizersForType(JobSpecification jobSpecification, SynchronizableFiles fileType)
    {
        _fileSynchronizers[fileType] = new Dictionary<string, IFileSynchronizer>(jobSpecification.Tasks.Count);

        foreach (var task in jobSpecification.Tasks)
        {
            var taskClusterDirectoryPath =
                FileSystemUtils.GetTaskClusterDirectoryPath(task, _scripts.SubExecutionsPath);
            var fileInfo = CreateSynchronizableFileInfoForType(task, taskClusterDirectoryPath, fileType);
            var sourceFilePath = FileSystemUtils.ConcatenatePaths(fileInfo.SourceDirectory, fileInfo.RelativePath);

            if (!_fileSynchronizers[fileType].ContainsKey(sourceFilePath))
                _fileSynchronizers[fileType][sourceFilePath] =
                    CreateFileSynchronizer(fileInfo, jobSpecification.ClusterUser);
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

    protected virtual ICollection<JobFileContent> SynchronizeAllFilesOfType(SynchronizableFiles fileType)
    {
        throw new NotImplementedException();
        //TODO: check if needed
        /*List<JobFileContent> results = new List<JobFileContent>();
        foreach (IFileSynchronizer synchronizer in _fileSynchronizers[fileType].Values)
        {
            ICollection<JobFileContent> result = synchronizer.SynchronizeFiles();
            if (result != null)
            {
                results.AddRange(result);
            }
        }
        return results;*/
    }

    #endregion
}
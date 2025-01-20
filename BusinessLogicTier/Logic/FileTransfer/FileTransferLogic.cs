using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using log4net;
using Renci.SshNet.Common;

namespace HEAppE.BusinessLogicTier.logic.FileTransfer;

public class FileTransferLogic : IFileTransferLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    public FileTransferLogic(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Unit of work
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    ///     _logger
    /// </summary>
    private readonly ILog _log;

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    #endregion

    #region Methods

    public void RemoveJobsTemporaryFileTransferKeys()
    {
        var activeTemporaryKeys = _unitOfWork.FileTransferTemporaryKeyRepository.GetAllActiveTemporaryKey()
            .Where(w => w.AddedAt.AddHours(BusinessLogicConfiguration.ValidityOfTemporaryTransferKeysInHours) <=
                        DateTime.UtcNow)
            .ToList();

        var activeTemporaryKeysGroup = activeTemporaryKeys.GroupBy(g => g.SubmittedJob.Specification.Cluster)
            .ToList();

        foreach (var activeTemporaryKeyGroup in activeTemporaryKeysGroup)
        {
            var cluster = activeTemporaryKeyGroup.Key;

            var clusterUserActiveTempKey = activeTemporaryKeyGroup.GroupBy(g =>
                    new
                    {
                        g.SubmittedJob.Specification.ClusterUser, g.SubmittedJob.Specification.Cluster,
                        g.SubmittedJob.Specification.Project
                    })
                .ToList();

            foreach (var tempKey in clusterUserActiveTempKey)
            {
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, tempKey.Key.Project);
                scheduler.RemoveDirectFileTransferAccessForUser(tempKey.Select(s => s.PublicKey),
                    tempKey.Key.ClusterUser, tempKey.Key.Cluster);
            }

            activeTemporaryKeyGroup.ToList().ForEach(f => f.IsDeleted = true);
            _unitOfWork.Save();
        }
    }

    public FileTransferMethod TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);

        var clusterUserAuthCredentials = jobInfo.Specification.ClusterUser;
        if (string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKey))
            throw new ClusterAuthenticationException("NotExistingPrivateKey", clusterUserAuthCredentials.PrivateKey);

        var transferMethod = new FileTransferMethod
        {
            Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
            Port = jobInfo.Specification.FileTransferMethod.Port,
            Cluster = jobInfo.Specification.Cluster,
            ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
            SharedBasePath =
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath),
            Credentials = new FileTransferKeyCredentials
            {
                Username = clusterUserAuthCredentials.Username,
                Password = clusterUserAuthCredentials.Password,
                FileTransferCipherType = clusterUserAuthCredentials.CipherType,
                CredentialsAuthType = clusterUserAuthCredentials.AuthenticationType,
                PrivateKey = clusterUserAuthCredentials.PrivateKey,
                PrivateKeyCertificate = clusterUserAuthCredentials.PrivateKeyCertificate,
                Passphrase = clusterUserAuthCredentials.PrivateKeyPassphrase
            }
        };
        return transferMethod;
    }

    public FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var cluster = jobInfo.Specification.Cluster;

        if (jobInfo.FileTransferTemporaryKeys.Count() > BusinessLogicConfiguration.GeneratedFileTransferKeyLimitPerJob)
            throw new FileTransferTemporaryKeyException("SshKeyGenerationLimit");

        var publicKey = string.Empty;
        var transferMethod = new FileTransferMethod
        {
            Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
            Port = jobInfo.Specification.FileTransferMethod.Port,
            Cluster = jobInfo.Specification.Cluster,
            ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
            SharedBasePath =
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath)
        };

        _log.Info($"Auth type: {jobInfo.Specification.ClusterUser.AuthenticationType}");
        if (jobInfo.Specification.ClusterUser.AuthenticationType ==
            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
        {
            var credentials =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetById(jobInfo.Specification.ClusterUser.Id);
            _log.Debug($"ClusterUser: {credentials}");
            transferMethod.Credentials = new FileTransferKeyCredentials
            {
                Username = jobInfo.Specification.ClusterUser.Username,
                FileTransferCipherType = credentials.CipherType,
                PrivateKey = credentials.PrivateKey,
                PrivateKeyCertificate = credentials.PrivateKeyCertificate,
                PublicKey = credentials.PublicKey
            };
            return transferMethod;
        }


        var certGenerator = new SSHGenerator();
        publicKey = certGenerator.ToPuTTYPublicKey();

        while (_unitOfWork.FileTransferTemporaryKeyRepository.ContainsActiveTemporaryKey(publicKey))
        {
            certGenerator.Regenerate();
            publicKey = certGenerator.ToPuTTYPublicKey();
        }

        transferMethod.Credentials = new FileTransferKeyCredentials
        {
            Username = jobInfo.Specification.ClusterUser.Username,
            FileTransferCipherType = certGenerator.CipherType,
            PrivateKey = certGenerator.ToPrivateKey(),
            CredentialsAuthType = ClusterAuthenticationCredentialsAuthType.PrivateKey, PublicKey = publicKey
        };


        jobInfo.FileTransferTemporaryKeys.Add(
            new FileTransferTemporaryKey
            {
                AddedAt = DateTime.UtcNow,
                PublicKey = publicKey
            });

        SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project)
            .AllowDirectFileTransferAccessForUserToJob(publicKey, jobInfo);

        _unitOfWork.Save();
        return transferMethod;
    }

    public void EndFileTransfer(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Removing file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var cluster = jobInfo.Specification.Cluster;

        if (jobInfo.Specification.ClusterUser.AuthenticationType is ClusterAuthenticationCredentialsAuthType
                .PrivateKeyInVaultAndInSshAgent) return;

        var temporaryKey = jobInfo.FileTransferTemporaryKeys.Find(f => f.PublicKey == publicKey);

        if (temporaryKey is null) throw new FileTransferTemporaryKeyException("PublicKeyMismatch");

        SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project)
            .RemoveDirectFileTransferAccessForUser(
                new[] { temporaryKey.PublicKey }, temporaryKey.SubmittedJob.Specification.ClusterUser,
                jobInfo.Specification.Cluster);

        temporaryKey.IsDeleted = true;
        _unitOfWork.Save();
    }

    public IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId,
        TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting part of job files from cluster for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var fileManager =
            FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
                .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);
        IList<JobFileContent> result = new List<JobFileContent>();
        foreach (var taskInfo in jobInfo.Tasks)
        {
            IList<TaskFileOffset> currentTaskFileOffsets = (from taskFileOffset in taskFileOffsets
                where taskFileOffset.SubmittedTaskInfoId == taskInfo.Id
                select taskFileOffset).ToList();
            foreach (var currentOffset in currentTaskFileOffsets)
            {
                var contents =
                    fileManager.DownloadPartOfJobFileFromCluster(taskInfo, currentOffset.FileType,
                        currentOffset.Offset);
                if (contents != null)
                    foreach (var content in contents)
                        result.Add(content);
            }
        }

        return result;
    }

    public IList<SynchronizedJobFiles> SynchronizeAllUnfinishedJobFiles()
    {
        var unfinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetNotFinishedJobInfos().ToList();

        var fileTransferMethodGroups =
            from jobInfo in unfinishedJobs
            group jobInfo by jobInfo.Specification.FileTransferMethod
            into fileTransferMethodGroup
            select fileTransferMethodGroup;
        IList<SynchronizedJobFiles> result = new List<SynchronizedJobFiles>(unfinishedJobs.Count);

        foreach (var fileTransferMethodGroup in fileTransferMethodGroups)
        {
            var fileManager = FileSystemFactory.GetInstance(fileTransferMethodGroup.Key.Protocol)
                .CreateFileSystemManager(fileTransferMethodGroup.Key);
            foreach (var jobInfo in fileTransferMethodGroup)
            {
                var synchronizationTime = DateTime.UtcNow;
                var files = fileManager.CopyLogFilesFromCluster(jobInfo);
                foreach (var file in fileManager.CopyProgressFilesFromCluster(jobInfo)) files.Add(file);
                foreach (var file in fileManager.CopyStdOutputFilesFromCluster(jobInfo)) files.Add(file);
                foreach (var file in fileManager.CopyStdErrorFilesFromCluster(jobInfo)) files.Add(file);
                var fileContents = new SynchronizedJobFiles
                {
                    SubmittedJobInfoId = jobInfo.Id,
                    SynchronizationTime = synchronizationTime,
                    FileContents = files.ToList()
                };
                result.Add(fileContents);
            }
        }

        return result;
    }

    public ICollection<FileInformation> ListChangedFilesForJob(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
            return null;
        var fileManager =
            FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
                .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);

        return fileManager.ListChangedFilesForJob(jobInfo, jobInfo.SubmitTime.Value);
    }

    public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser)
    {
        var jobInfo = LogicFactory.GetLogicFactory()
            .CreateJobManagementLogic(_unitOfWork)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);

        var fileManager = FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
            .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);

        if (jobInfo.State == JobState.Deleted)
        {
            return HandleDeletedJobFileDownload(jobInfo, relativeFilePath, fileManager, loggedUser);
        }

        if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
        {
            _log.Error(
                $"Job is not submitted or is waiting for service account, cannot download file for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
            return null;
        }

        return HandleActiveJobFileDownload(jobInfo, relativeFilePath, fileManager, loggedUser);
    }

    private byte[] HandleDeletedJobFileDownload(
        SubmittedJobInfo jobInfo,
        string relativeFilePath,
        IRexFileSystemManager fileManager,
        AdaptorUser loggedUser)
    {
        _log.Info($"Getting file from archive for submitted job Id {jobInfo.Id} with user {loggedUser.GetLogIdentification()}");

        try
        {
            relativeFilePath = relativeFilePath.TrimStart('/');
            var localBasePath = GetLocalArchiveBasePath(jobInfo);

            foreach (var task in jobInfo.Tasks)
            {
                var paths = GetTaskPathVariants(jobInfo, task);

                foreach (var path in paths)
                {
                    if (relativeFilePath.StartsWith(path))
                    {
                        try
                        {
                            var fullPath = Path.Combine(localBasePath, relativeFilePath.TrimStart('/'));
                            return fileManager.DownloadFileFromClusterByAbsolutePath(jobInfo.Specification, fullPath);
                        }
                        catch (Exception ex)
                        {
                            _log.Info(
                                $"File not found in cluster, trying to get it from archive for submitted job Id {jobInfo.Id} with user {loggedUser.GetLogIdentification()}, {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (SftpPathNotFoundException exception)
        {
            throw new InvalidRequestException("NotExistingPathInArchive", relativeFilePath, exception.Message);
        }

        return null;
    }

    private byte[] HandleActiveJobFileDownload(
        SubmittedJobInfo jobInfo,
        string relativeFilePath,
        IRexFileSystemManager fileManager,
        AdaptorUser loggedUser)
    {
        _log.Info($"Getting file from cluster for submitted job Id {jobInfo.Id} with user {loggedUser.GetLogIdentification()}");

        relativeFilePath = relativeFilePath.TrimStart('/');

        foreach (var task in jobInfo.Tasks)
        {
            var paths = GetTaskPathVariants(jobInfo, task);

            foreach (var path in paths)
            {
                if (relativeFilePath.StartsWith(path))
                {
                    try
                    {
                        return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(
                            $"File not found in cluster for submitted job Id {jobInfo.Id} with user {loggedUser.GetLogIdentification()}, {ex.Message}");
                    }
                }
            }
        }

        try
        {
            return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath);
        }
        catch (SftpPathNotFoundException exception)
        {
            throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
        }
    }

    private string GetLocalArchiveBasePath(SubmittedJobInfo jobInfo)
    {
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.LocalBasepath;

        return Path.Combine(basePath, _scripts.JobLogArchiveSubPath.TrimStart('/'));
    }

    private IEnumerable<string> GetTaskPathVariants(SubmittedJobInfo jobInfo, SubmittedTaskInfo task)
    {
        var taskSubdirectory = task.Specification.ClusterTaskSubdirectory ?? string.Empty;

        yield return Path.Combine($"{jobInfo.Id}", $"{task.Id}", taskSubdirectory);
        yield return Path.Combine($"{task.Id}", taskSubdirectory);
    }


    public virtual FileTransferMethod GetFileTransferMethodById(long fileTransferMethodById)
    {
        return _unitOfWork.FileTransferMethodRepository.GetById(fileTransferMethodById)
               ?? throw new RequestedObjectDoesNotExistException("NotExistingFileTransferMethod",
                   fileTransferMethodById);
    }

    public virtual IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId)
    {
        return _unitOfWork.FileTransferMethodRepository.GetByClusterId(clusterId)
            .ToList();
    }

    #endregion
}
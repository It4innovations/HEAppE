using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.Migrations;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;
using HEAppE.Services.UserOrg;
using HEAppE.Utils;
using log4net;
using Renci.SshNet.Common;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.logic.FileTransfer;

public class FileTransferLogic : IFileTransferLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    internal FileTransferLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _userOrgService = userOrgService;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Unit of work
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;
    
    /// <summary>
    /// User Org Service
    /// </summary>
    private readonly IUserOrgService _userOrgService;

    /// <summary>
    ///     _logger
    /// </summary>
    private readonly ILog _log;
    
    /// <summary>
    /// Ssh CA service
    /// </summary>
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    
    /// <summary>
    /// HTTP Context Keys
    /// </summary>
    private readonly IHttpContextKeys _httpContextKeys;

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
                        g.SubmittedJob.Specification.ClusterUser,
                        g.SubmittedJob.Specification.Cluster,
                        g.SubmittedJob.Specification.Project
                    })
                .ToList();

            foreach (var tempKey in clusterUserActiveTempKey)
            {
                var clusterUser = tempKey.Key.ClusterUser;
                var userName = tempKey.Key.ClusterUser?.Username ?? "Unknown User";
                var clusterName = tempKey.Key.Cluster?.Name ?? "Unknown Cluster";
                
                if (clusterUser == null)
                {
                    _log.Warn(
                        $"Cluster user is null for temporary file transfer key(s) in cluster \"{clusterName}\". Skipping removal of file transfer keys for this user.");
                    continue;
                }

                _log.Info(
                    $"Removing file transfer key for user \"{userName}\" in cluster \"{clusterName}\"");
                
                long? adaptorUserId = (tempKey.Key.Project?.IsOneToOneMapping == true)
                    ? tempKey.Key.ClusterUser?.ClusterProjectCredentials?.FirstOrDefault()?.AdaptorUser?.Id
                    : null;
                
                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, tempKey.Key.Project, _sshCertificateAuthorityService, adaptorUserId: adaptorUserId);
                scheduler.RemoveDirectFileTransferAccessForUser(tempKey.Select(s => s.PublicKey),
                    tempKey.Key.ClusterUser, tempKey.Key.Cluster, tempKey.Key.Project, _httpContextKeys.Context.SshCaToken);
            }

            activeTemporaryKeyGroup.ToList().ForEach(f => f.IsDeleted = true);
            _unitOfWork.Save();
        }
    }

    public async Task<FileTransferMethod> TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);

        var clusterUserAuthCredentials = jobInfo.Specification.ClusterUser;
        //retrieve credentials from vault
        clusterUserAuthCredentials = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetByIdAsync(clusterUserAuthCredentials.Id);
        if (string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKey))
            throw new ClusterAuthenticationException("NotExistingPrivateKey", clusterUserAuthCredentials.PrivateKey);
        
        SignResponse response = new SignResponse();
        string publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(clusterUserAuthCredentials).PublicKeyInAuthorizedKeysFormat;
        if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            response = await _sshCertificateAuthorityService
                .SignAsync(publicKey, _httpContextKeys.Context.SshCaToken,
                    jobInfo.Specification.FileTransferMethod.ServerHostname);

        }

        var transferMethod = new FileTransferMethod
        {
            Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
            Port = jobInfo.Specification.FileTransferMethod.Port,
            Cluster = jobInfo.Specification.Cluster,
            ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
            SharedBasePath =
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath),
            Credentials = new FileTransferKeyCredentials
            {
                Username = (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication && SshCaSettings.UsePosixAccountFromCertificate) ? response.PosixUsername : clusterUserAuthCredentials.Username,
                Password = clusterUserAuthCredentials.Password,
                FileTransferCipherType = clusterUserAuthCredentials.CipherType,
                CredentialsAuthType = clusterUserAuthCredentials.AuthenticationType,
                PrivateKey = clusterUserAuthCredentials.PrivateKey,
                PrivateKeyCertificate = (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication) ? response.SshCert : string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKeyCertificate)? null : clusterUserAuthCredentials.PrivateKeyCertificate,
                Passphrase = clusterUserAuthCredentials.PrivateKeyPassphrase
            }
        };
        
        return transferMethod;
    }

    public async Task<FileTransferMethod> GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys)
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
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath)
        };

        _log.Info($"Auth type: {jobInfo.Specification.ClusterUser.AuthenticationType}");
        if (jobInfo.Specification.ClusterUser.AuthenticationType ==
            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
        {
            var credentials =
                await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetByIdAsync(jobInfo.Specification.ClusterUser.Id);
            _log.Debug($"ClusterUser: {credentials}");
            transferMethod.Credentials = new FileTransferKeyCredentials
            {
                Username = jobInfo.Specification.ClusterUser.Username,
                FileTransferCipherType = credentials.CipherType,
                PrivateKey = credentials.PrivateKey,
                PrivateKeyCertificate = string.IsNullOrEmpty(credentials.PrivateKeyCertificate)? null : credentials.PrivateKeyCertificate,
                PublicKey = credentials.PublicKey
            };
            return transferMethod;
        }


        var certGenerator = new SSHGenerator();
        publicKey = certGenerator.ToPuTTYPublicKey("");

        while (_unitOfWork.FileTransferTemporaryKeyRepository.ContainsActiveTemporaryKey(publicKey))
        {
            certGenerator.Regenerate();
            publicKey = certGenerator.ToPuTTYPublicKey("");
        }

        SignResponse response = new SignResponse();
        if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            response = await _sshCertificateAuthorityService
                .SignAsync(publicKey, _httpContextKeys.Context.SshCaToken, transferMethod.ServerHostname);
        }

        transferMethod.Credentials = new FileTransferKeyCredentials
        {
            Username = (!SshCaSettings.UsePosixAccountFromCertificate || string.IsNullOrEmpty(response.PosixUsername))?jobInfo.Specification.ClusterUser.Username: response.PosixUsername,
            FileTransferCipherType = certGenerator.CipherType,
            PrivateKey = certGenerator.CipherType != FileTransferCipherType.Ed25519 ? certGenerator.ToPrivateKey() : certGenerator.ToPrivateKeyInPEM(),
            CredentialsAuthType = ClusterAuthenticationCredentialsAuthType.PrivateKey, 
            PublicKey = publicKey,
            PrivateKeyCertificate = response.SshCert
        };


        jobInfo.FileTransferTemporaryKeys.Add(
            new FileTransferTemporaryKey
            {
                AddedAt = DateTime.UtcNow,
                PublicKey = publicKey
            });

        SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService,adaptorUserId: loggedUser.Id)
            .AllowDirectFileTransferAccessForUserToJob(publicKey, jobInfo, _httpContextKeys.Context.SshCaToken);

        await _unitOfWork.SaveAsync();
        return transferMethod;
    }

    public void EndFileTransfer(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Removing file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var cluster = jobInfo.Specification.Cluster;

        if (jobInfo.Specification.ClusterUser.AuthenticationType is ClusterAuthenticationCredentialsAuthType
                .PrivateKeyInVaultAndInSshAgent) return;

        var temporaryKey = jobInfo.FileTransferTemporaryKeys.Find(f => f.PublicKey == publicKey);

        if (temporaryKey is null) throw new FileTransferTemporaryKeyException("PublicKeyMismatch");

        SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
            .RemoveDirectFileTransferAccessForUser(
                new[] { temporaryKey.PublicKey }, temporaryKey.SubmittedJob.Specification.ClusterUser,
                jobInfo.Specification.Cluster, jobInfo.Project, _httpContextKeys.Context.SshCaToken);

        temporaryKey.IsDeleted = true;
        _unitOfWork.Save();
    }

    public IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId,
        TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
    {
        _log.Info(
            $"Getting part of job files from cluster for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var fileManager =
            FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
                .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod, _sshCertificateAuthorityService);
        IList<JobFileContent> result = new List<JobFileContent>();
        
        foreach (var taskInfo in jobInfo.Tasks)
        {
            IList<TaskFileOffset> currentTaskFileOffsets = (from taskFileOffset in taskFileOffsets
                where taskFileOffset.SubmittedTaskInfoId == taskInfo.Id
                select taskFileOffset).ToList();
            
            foreach (var currentOffset in currentTaskFileOffsets)
            {
                ICollection<JobFileContent> contents = null;
                if (jobInfo.State == JobState.Deleted)
                {
                    contents =
                        fileManager.DownloadPartOfJobFileFromCluster(taskInfo, currentOffset.FileType,
                            currentOffset.Offset, _scripts.InstanceIdentifierPath, _scripts.JobLogArchiveSubPath, _httpContextKeys.Context.SshCaToken);
                }
                else
                {
                    contents =
                        fileManager.DownloadPartOfJobFileFromCluster(taskInfo, currentOffset.FileType,
                            currentOffset.Offset, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath, _httpContextKeys.Context.SshCaToken);
                }

                if (contents != null)
                {
                    foreach (var content in contents)
                    {
                        result.Add(content); 
                    }
                } 
            }
        }

        return result;
    }

    public IList<SynchronizedJobFiles> SynchronizeAllUnfinishedJobFiles()
    {
        var unfinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
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
                .CreateFileSystemManager(fileTransferMethodGroup.Key, _sshCertificateAuthorityService);
            foreach (var jobInfo in fileTransferMethodGroup)
            {
                var synchronizationTime = DateTime.UtcNow;
                var files = fileManager.CopyLogFilesFromCluster(jobInfo, _httpContextKeys.Context.SshCaToken);
                foreach (var file in fileManager.CopyProgressFilesFromCluster(jobInfo, _httpContextKeys.Context.SshCaToken)) files.Add(file);
                foreach (var file in fileManager.CopyStdOutputFilesFromCluster(jobInfo, _httpContextKeys.Context.SshCaToken)) files.Add(file);
                foreach (var file in fileManager.CopyStdErrorFilesFromCluster(jobInfo, _httpContextKeys.Context.SshCaToken)) files.Add(file);
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
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        var fileManager =
            FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
                .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod, _sshCertificateAuthorityService);

        if(jobInfo.State == JobState.Deleted)
        {
            return fileManager.ListArchivedFilesForJob(jobInfo, jobInfo.CreationTime, _httpContextKeys.Context.SshCaToken);
        }
        
        if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
            return null;
       
        return fileManager.ListChangedFilesForJob(jobInfo, jobInfo.CreationTime, _httpContextKeys.Context.SshCaToken);
    }
    public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser)
    {
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        
        var fileManager =
            FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol)
                .CreateFileSystemManager(jobInfo.Specification.FileTransferMethod, _sshCertificateAuthorityService);
        
        if (jobInfo.State == JobState.Deleted)
        {
            return HandleDeletedJobFileDownload(jobInfo, relativeFilePath, fileManager, loggedUser);
        }
        
        if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
            return null;
        
        try
        {
            relativeFilePath = relativeFilePath.TrimStart('/');
            foreach (var task in jobInfo.Tasks)
            {
                var start1 = Path.Combine($"{jobInfo.Specification.Id}", $"{task.Specification.Id}",
                    $"{task.Specification.ClusterTaskSubdirectory ?? string.Empty}");
                var start2 = Path.Combine($"{task.Specification.Id}",
                    $"{task.Specification.ClusterTaskSubdirectory ?? string.Empty}");
                if (relativeFilePath.StartsWith(start1))
                {
                    try
                    {
                        return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }

                if (relativeFilePath.StartsWith(start2))
                {
                    try
                    {
                        relativeFilePath = Path.Combine($"{jobInfo.Specification.Id}", relativeFilePath.TrimStart('/'));
                        return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }
            }
            return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken);
        }
        catch (SftpPathNotFoundException exception)
        {
            throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
        }
    }

    private (FileTransferMethod, FileTransferProtocol?) GetFileTransferMethodForUpload(long clusterId)
    {
        FileTransferMethod fileTransferMethod = null;
        FileTransferProtocol? fileTransferProtocol = null;
        var clusterFileTransferMethods = GetFileTransferMethodsByClusterId(clusterId);
        foreach (var protocol in new[] { FileTransferProtocol.LocalSftpScp, FileTransferProtocol.SftpScp, FileTransferProtocol.NetworkShare })
        {
            var method = clusterFileTransferMethods.Where(ftm => ftm.Protocol == protocol);
            if (method.Any())
            {
                fileTransferMethod = method.First();
                fileTransferProtocol = protocol;
                break;
            }
        }
        return (fileTransferMethod, fileTransferProtocol);
    }

    public async Task<dynamic> UploadFileToProjectDir(Stream fileStream, string fileName, long projectId,
        long clusterId, AdaptorUser loggedUser)
    {
        var result = new Dictionary<string, dynamic>();
        
        fileName = FileSystemUtils.SanitizeFileName(fileName);
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
        var cluster = clusterProject.Cluster;
        
        //invoke user information logic and run GetNextAvailableUserCredentials
        var logic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        var credentials = await logic.GetNextAvailableUserCredentials(clusterId, projectId, true, loggedUser.Id);

        var (fileTransferMethod, fileTransferProtocol) = GetFileTransferMethodForUpload(clusterId);
        if (fileTransferMethod == null)
            return result;

        var projectStoragePath = clusterProject.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectStoragePath))
            throw new InvalidRequestException("ProjectPathNotSet");

        var absoluteFilePath = FileSystemUtils.SanitizePath(FileSystemUtils.ConcatenatePaths(clusterProject.ProjectStoragePath, fileName));
        var fileManager = FileSystemFactory.GetInstance(fileTransferProtocol.Value).CreateFileSystemManager(fileTransferMethod, _sshCertificateAuthorityService);
        var succeeded = fileManager.UploadFileToClusterByAbsolutePath(fileStream, absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken);
        result.Add("Succeeded", succeeded);
        result.Add("Path", succeeded ? absoluteFilePath : null);
        return result;
    }

    public async Task<dynamic> UploadJobScriptToProjectDir(Stream fileStream, string fileName, long projectId,
        long clusterId, AdaptorUser loggedUser)
    {
        var result = new Dictionary<string, dynamic>();

        fileName = FileSystemUtils.SanitizeFileName(fileName);
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
        var cluster = clusterProject.Cluster;
        
        var logic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        var credentials = await logic.GetNextAvailableUserCredentials(clusterId, projectId, true, loggedUser.Id);
        
        
        var (fileTransferMethod, fileTransferProtocol) = GetFileTransferMethodForUpload(clusterId);
        if (fileTransferMethod == null)
            return result;

        var projectStoragePath = clusterProject.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectStoragePath))
            throw new InvalidRequestException("ProjectPathNotSet");

        var absoluteFilePath = FileSystemUtils.SanitizePath(FileSystemUtils.ConcatenatePaths(projectStoragePath, fileName));
        var fileManager = FileSystemFactory.GetInstance(fileTransferProtocol.Value).CreateFileSystemManager(fileTransferMethod, _sshCertificateAuthorityService);
        var succeeded = fileManager.UploadFileToClusterByAbsolutePath(fileStream, absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken);
        bool attributesSet = false;
        if (succeeded)
        {
            attributesSet = fileManager.ModifyAbsolutePathFileAttributes(absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken,
                ownerCanExecute: true, groupCanExecute: true);
        }
        result.Add("Succeeded", succeeded);
        result.Add("Path", succeeded ? absoluteFilePath : null);
        result.Add("AttributesSet", attributesSet);
        return result;
    }

    public dynamic UploadFileToJobExecutionDir(Stream fileStream, string fileName, long createdJobInfoId, long? createdTaskInfoId, AdaptorUser loggedUser)
    {
        var result = new Dictionary<string, dynamic>();
        
        var jobSpecification = _unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId);
        var jobClusterDirectoryPath = FileSystemUtils
            .GetJobClusterDirectoryPath(jobSpecification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath);
        if (string.IsNullOrEmpty(jobClusterDirectoryPath))
            throw new Exception("Error: jobClusterDirectoryPath is not set!");

        string absoluteFilePath = string.Empty;
        if (createdTaskInfoId.HasValue)
        {
            string path = Path.Combine(jobClusterDirectoryPath, createdTaskInfoId.Value.ToString());
            absoluteFilePath = FileSystemUtils.ConcatenatePaths(path, FileSystemUtils.SanitizeFileName(fileName));
        }
        else
        {
            absoluteFilePath = FileSystemUtils.ConcatenatePaths(jobClusterDirectoryPath, FileSystemUtils.SanitizeFileName(fileName));
        }
        
        absoluteFilePath = FileSystemUtils.SanitizePath(absoluteFilePath);

        var fileManager = FileSystemFactory.GetInstance(jobSpecification.FileTransferMethod.Protocol)
            .CreateFileSystemManager(jobSpecification.FileTransferMethod, _sshCertificateAuthorityService);
        var succeeded = fileManager.UploadFileToClusterByAbsolutePath(fileStream, absoluteFilePath, jobSpecification.ClusterUser, jobSpecification.Cluster, _httpContextKeys.Context.SshCaToken);
        result.Add("Succeeded", succeeded);
        result.Add("Path", succeeded ? absoluteFilePath : null);
        return result;
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
            foreach (var task in jobInfo.Tasks)
            {
                var start1 = Path.Combine($"{jobInfo.Specification.Id}", $"{task.Specification.Id}",
                    $"{task.Specification.ClusterTaskSubdirectory ?? string.Empty}");
                var start2 = Path.Combine($"{task.Specification.Id}",
                    $"{task.Specification.ClusterTaskSubdirectory ?? string.Empty}");
                
                var basePath = jobInfo.Specification.Cluster.ClusterProjects
                    .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ProjectStoragePath;
                if (string.IsNullOrEmpty(basePath))
                {
                    basePath = jobInfo.Specification.Cluster.ClusterProjects
                        .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
                }
                var localBasePath = Path.Combine(basePath, _scripts.InstanceIdentifierPath, _scripts.JobLogArchiveSubPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username);
 
                if (relativeFilePath.StartsWith(start1))
                {
                    try
                    {
                        var file = Path.Combine(localBasePath, relativeFilePath.TrimStart('/'));
                        return fileManager.DownloadFileFromClusterByAbsolutePath(jobInfo.Specification, file, _httpContextKeys.Context.SshCaToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }

                if (relativeFilePath.StartsWith(start2))
                {
                    try
                    {
                        relativeFilePath = Path.Combine($"{jobInfo.Specification.Id}", relativeFilePath.TrimStart('/'));
                        var file = Path.Combine(localBasePath, relativeFilePath.TrimStart('/'));
                        return fileManager.DownloadFileFromClusterByAbsolutePath(jobInfo.Specification, file, _httpContextKeys.Context.SshCaToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }
            }
            return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken);
        }
        catch (SftpPathNotFoundException exception)
        {
            throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
        }
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
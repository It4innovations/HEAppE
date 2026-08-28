using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
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
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;
using Renci.SshNet.Common;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer;

public class FileTransferLogic : IFileTransferLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    internal FileTransferLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _userOrgService = userOrgService;
        _expirioService = expirioService;
        _logger = logger;
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
    /// Expirio service
    /// </summary>
    private readonly IExpirioService _expirioService;

    /// <summary>
    ///     _logger
    /// </summary>
    private readonly ILogger _logger;
    
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

    public async Task RemoveJobsTemporaryFileTransferKeysAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(-BusinessLogicConfiguration.ValidityOfTemporaryTransferKeysInHours);
        var activeTemporaryKeys = _unitOfWork.FileTransferTemporaryKeyRepository.GetAllActiveTemporaryKeyExpiredBefore(threshold);

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
                    _logger.LogWarning(
                        $"Cluster user is null for temporary file transfer key(s) in cluster \"{clusterName}\". Skipping removal of file transfer keys for this user.");
                    continue;
                }

                try
                {
                    if (clusterUser.ClusterProjectCredentials?.FirstOrDefault()?.AdaptorUser != null)
                    {
                        var au = clusterUser.ClusterProjectCredentials.FirstOrDefault().AdaptorUser;
                        HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(au.Id, au.Username, au.Email);
                    }

                    _logger.LogInformation(
                        $"Removing file transfer key for user \"{userName}\" in cluster \"{clusterName}\"");

                    long? adaptorUserId = (tempKey.Key.Project?.IsOneToOneMapping == true)
                        ? tempKey.Key.ClusterUser?.ClusterProjectCredentials?.FirstOrDefault()?.AdaptorUser?.Id
                        : null;

                    var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                        .CreateScheduler(cluster, tempKey.Key.Project, _sshCertificateAuthorityService,
                            adaptorUserId: adaptorUserId, _expirioService, _expirioToken, _logger);
                    await scheduler.RemoveDirectFileTransferAccessForUserAsync(tempKey.Select(s => s.PublicKey),
                        tempKey.Key.ClusterUser, tempKey.Key.Cluster, tempKey.Key.Project,
                        _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                }
                finally
                {
                    HEAppE.Utils.LoggingUtils.RemoveUserPropertiesFromLogThreadContext();
                }
            }

            activeTemporaryKeyGroup.ToList().ForEach(f => f.IsDeleted = true);
            await _unitOfWork.SaveAsync();
        }
    }

    public async Task<FileTransferMethod> TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        _logger.LogInformation(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);

        var clusterUserAuthCredentials = jobInfo.Specification.ClusterUser;
        //retrieve credentials from vault
        clusterUserAuthCredentials = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetByIdAsync(clusterUserAuthCredentials.Id);
        clusterUserAuthCredentials.SessionUserId = loggedUser.Id;
        if (string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKey))
            throw new ClusterAuthenticationException("NotExistingPrivateKey", clusterUserAuthCredentials.PrivateKey);
        
        SignResponse response = new SignResponse();
        string publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(clusterUserAuthCredentials).PublicKeyInAuthorizedKeysFormat;
        if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            var clusterObj = jobInfo.Specification.Cluster;
            var resourceName = !string.IsNullOrEmpty(clusterObj?.MasterNodeName)
                ? clusterObj.MasterNodeName
                : (jobInfo.Specification.FileTransferMethod.ServerHostname ?? clusterObj?.Name);

            response = await _sshCertificateAuthorityService
                .SignAsync(publicKey, _httpContextKeys.Context.SshCaToken, resourceName, _logger);
        }

        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var transferMethod = new FileTransferMethod
        {
            Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
            Port = jobInfo.Specification.FileTransferMethod.Port,
            Cluster = jobInfo.Specification.Cluster,
            ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
            SharedBasePath =
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath),
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
        _logger.LogInformation(
            $"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var cluster = jobInfo.Specification.Cluster;

        if (jobInfo.FileTransferTemporaryKeys.Count() > BusinessLogicConfiguration.GeneratedFileTransferKeyLimitPerJob)
            throw new FileTransferTemporaryKeyException("SshKeyGenerationLimit");

        var publicKey = string.Empty;
        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var transferMethod = new FileTransferMethod
        {
            Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
            Port = jobInfo.Specification.FileTransferMethod.Port,
            Cluster = jobInfo.Specification.Cluster,
            ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
            SharedBasePath =
                FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath)
        };

        _logger.LogInformation($"Auth type: {jobInfo.Specification.ClusterUser.AuthenticationType}");
        if (jobInfo.Specification.ClusterUser.AuthenticationType ==
            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
        {
            var credentials =
                await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetByIdAsync(jobInfo.Specification.ClusterUser.Id);
            credentials.SessionUserId = loggedUser.Id;
            _logger.LogDebug($"ClusterUser: {credentials}");
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


        var certGenerator = new SSHGenerator(_logger);
        publicKey = certGenerator.ToPuTTYPublicKey("");

        while (_unitOfWork.FileTransferTemporaryKeyRepository.ContainsActiveTemporaryKey(publicKey))
        {
            certGenerator.Regenerate();
            publicKey = certGenerator.ToPuTTYPublicKey("");
        }

        SignResponse response = new SignResponse();
        if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            var resourceName = !string.IsNullOrEmpty(cluster.MasterNodeName)
                ? cluster.MasterNodeName
                : (transferMethod.ServerHostname ?? cluster.Name);

            response = await _sshCertificateAuthorityService
                .SignAsync(publicKey, _httpContextKeys.Context.SshCaToken, resourceName, _logger);
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

        await SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService,adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .AllowDirectFileTransferAccessForUserToJobAsync(publicKey, jobInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        await _unitOfWork.SaveAsync();
        return transferMethod;
    }

    public async System.Threading.Tasks.Task EndFileTransferAsync(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser)
    {
        _logger.LogInformation(
            $"Removing file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var cluster = jobInfo.Specification.Cluster;

        if (jobInfo.Specification.ClusterUser.AuthenticationType is ClusterAuthenticationCredentialsAuthType
                .PrivateKeyInVaultAndInSshAgent) return;

        var temporaryKey = jobInfo.FileTransferTemporaryKeys.Find(f => f.PublicKey == publicKey);

        if (temporaryKey is null) throw new FileTransferTemporaryKeyException("PublicKeyMismatch");

        await SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _expirioToken, _logger)
            .RemoveDirectFileTransferAccessForUserAsync(
                new[] { temporaryKey.PublicKey }, temporaryKey.SubmittedJob.Specification.ClusterUser,
                jobInfo.Specification.Cluster, jobInfo.Project, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);

        temporaryKey.IsDeleted = true;
        await _unitOfWork.SaveAsync();
    }

    public async Task<IList<JobFileContent>> DownloadPartsOfJobFilesFromClusterAsync(long submittedJobInfoId,
        TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
    {
        _logger.LogInformation(
            $"Getting part of job files from cluster for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var fileManager = CreateFileSystemManager(jobInfo.Specification);
        IList<JobFileContent> result = new List<JobFileContent>();
        
        foreach (var taskInfo in jobInfo.Tasks)
        {
            IList<TaskFileOffset> currentTaskFileOffsets = (from taskFileOffset in taskFileOffsets
                where taskFileOffset.SubmittedTaskInfoId == taskInfo.Id
                select taskFileOffset).ToList();
            
            var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
            foreach (var currentOffset in currentTaskFileOffsets)
            {
                ICollection<JobFileContent> contents = null;
                if (jobInfo.State == JobState.Deleted)
                {
                    contents =
                        await fileManager.DownloadPartOfJobFileFromClusterAsync(taskInfo, currentOffset.FileType,
                            currentOffset.Offset, clusterConfig.InstanceIdentifierPath, clusterConfig.JobLogArchiveSubPath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                }
                else
                {
                    contents =
                        await fileManager.DownloadPartOfJobFileFromClusterAsync(taskInfo, currentOffset.FileType,
                            currentOffset.Offset, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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

    public async Task<IList<SynchronizedJobFiles>> SynchronizeAllUnfinishedJobFilesAsync()
    {
        var unfinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetNotFinishedJobInfos().ToList();

        var fileTransferMethodGroups =
            from jobInfo in unfinishedJobs
            group jobInfo by jobInfo.Specification.FileTransferMethod
            into fileTransferMethodGroup
            select fileTransferMethodGroup;
        IList<SynchronizedJobFiles> result = new List<SynchronizedJobFiles>(unfinishedJobs.Count);

        foreach (var fileTransferMethodGroup in fileTransferMethodGroups)
        {
            var firstJob = fileTransferMethodGroup.First();
            var fileManager = CreateFileSystemManager(firstJob.Specification);
            foreach (var jobInfo in fileTransferMethodGroup)
            {
                var synchronizationTime = DateTime.UtcNow;
                var files = await fileManager.CopyLogFilesFromClusterAsync(jobInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                foreach (var file in await fileManager.CopyProgressFilesFromClusterAsync(jobInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken)) files.Add(file);
                foreach (var file in await fileManager.CopyStdOutputFilesFromClusterAsync(jobInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken)) files.Add(file);
                foreach (var file in await fileManager.CopyStdErrorFilesFromClusterAsync(jobInfo, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken)) files.Add(file);
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

    public async Task<ICollection<FileInformation>> ListChangedFilesForJobAsync(long submittedJobInfoId, AdaptorUser loggedUser)
    {
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        var fileManager = CreateFileSystemManager(jobInfo.Specification);

        if (jobInfo.State == JobState.Deleted)
        {
            var archivedFiles = await fileManager.ListArchivedFilesForJobAsync(jobInfo, jobInfo.CreationTime, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
            return archivedFiles?.Where(f => !IsPathBlocked(f.FileName)).ToList();
        }
        
        if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
            return null;
       
        var files = await fileManager.ListChangedFilesForJobAsync(jobInfo, jobInfo.CreationTime, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        return files?.Where(f => !IsPathBlocked(f.FileName)).ToList();
    }
    public async Task<byte[]> DownloadFileFromClusterAsync(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser)
    {
        if (IsPathBlocked(relativeFilePath))
        {
            throw new NotAllowedException("PathAccessDenied");
        }

        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);
        
        var fileManager = CreateFileSystemManager(jobInfo.Specification);
        
        if (jobInfo.State == JobState.Deleted)
        {
            return await HandleDeletedJobFileDownloadAsync(jobInfo, relativeFilePath, fileManager, loggedUser);
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
                        return await fileManager.DownloadFileFromClusterAsync(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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
                        return await fileManager.DownloadFileFromClusterAsync(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }
            }
            return await fileManager.DownloadFileFromClusterAsync(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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

        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
        if (cluster != null && cluster.SchedulerType == SchedulerType.QScheduler &&
            (cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.Ssh) ||
             cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.SshInteractive)))
        {
            var method = clusterFileTransferMethods.FirstOrDefault(ftm => ftm.Protocol == FileTransferProtocol.Http || ftm.Protocol == FileTransferProtocol.Https);
            if (method != null)
            {
                fileTransferMethod = new FileTransferMethod
                {
                    Id = method.Id,
                    ServerHostname = cluster.MasterNodeName,
                    Port = cluster.Port,
                    Protocol = FileTransferProtocol.SftpScp,
                    ClusterId = cluster.Id,
                    Cluster = cluster,
                    Credentials = method.Credentials,
                    IsDeleted = false
                };
                fileTransferProtocol = FileTransferProtocol.SftpScp;
                return (fileTransferMethod, fileTransferProtocol);
            }
        }

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
        var logic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        var credentials = await logic.GetNextAvailableUserCredentials(clusterId, projectId, true, loggedUser.Id);

        var (fileTransferMethod, fileTransferProtocol) = GetFileTransferMethodForUpload(clusterId);
        if (fileTransferMethod == null)
            return result;

        var projectStoragePath = clusterProject.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectStoragePath))
            throw new InvalidRequestException("ProjectPathNotSet");

        var absoluteFilePath = FileSystemUtils.SanitizePath(FileSystemUtils.ConcatenatePaths(clusterProject.ProjectStoragePath, fileName));
        var fileManager = FileSystemFactory.GetInstance(fileTransferProtocol.Value, cluster).CreateFileSystemManager(fileTransferMethod, _sshCertificateAuthorityService, _logger);
        var succeeded = await fileManager.UploadFileToClusterByAbsolutePathAsync(fileStream, absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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
        
        var logic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        var credentials = await logic.GetNextAvailableUserCredentials(clusterId, projectId, true, loggedUser.Id);
        
        
        var (fileTransferMethod, fileTransferProtocol) = GetFileTransferMethodForUpload(clusterId);
        if (fileTransferMethod == null)
            return result;

        var projectStoragePath = clusterProject.ProjectStoragePath;
        if (string.IsNullOrEmpty(projectStoragePath))
            throw new InvalidRequestException("ProjectPathNotSet");

        var absoluteFilePath = FileSystemUtils.SanitizePath(FileSystemUtils.ConcatenatePaths(projectStoragePath, fileName));
        var fileManager = FileSystemFactory.GetInstance(fileTransferProtocol.Value, cluster).CreateFileSystemManager(fileTransferMethod, _sshCertificateAuthorityService, _logger);
        var succeeded = await fileManager.UploadFileToClusterByAbsolutePathAsync(fileStream, absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        bool attributesSet = false;
        if (succeeded)
        {
            attributesSet = await fileManager.ModifyAbsolutePathFileAttributesAsync(absoluteFilePath, credentials, cluster, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken,
                ownerCanExecute: true, groupCanExecute: true);
        }
        result.Add("Succeeded", succeeded);
        result.Add("Path", succeeded ? absoluteFilePath : null);
        result.Add("AttributesSet", attributesSet);
        return result;
    }

    public async Task<dynamic> UploadFileToJobExecutionDirAsync(Stream fileStream, string fileName, long submittedJobInfoId, long? submittedTaskInfoId, AdaptorUser loggedUser)
    {
        if (IsPathBlocked(fileName))
        {
            throw new NotAllowedException("PathAccessDenied");
        }

        var result = new Dictionary<string, dynamic>();
        
        var jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        VerifyOwner(jobInfo, loggedUser);

        var jobSpecification = jobInfo.Specification;
        var clusterConfig = ClusterRuntimeConfiguration.For(jobSpecification.Cluster.CustomConfiguration);
        var jobClusterDirectoryPath = FileSystemUtils
            .GetJobClusterDirectoryPath(jobSpecification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath);
        if (string.IsNullOrEmpty(jobClusterDirectoryPath))
            throw new Exception("Error: jobClusterDirectoryPath is not set!");

        string absoluteFilePath = string.Empty;
        if (submittedTaskInfoId.HasValue)
        {
            var taskInfo = jobInfo.Tasks.FirstOrDefault(t => t.Id == submittedTaskInfoId.Value) ??
                           throw new Exception("TaskDoesNotBelongToJob");
            var taskSpecificationId = taskInfo.Specification.Id;
            string path = Path.Combine(jobClusterDirectoryPath, taskSpecificationId.ToString());
            absoluteFilePath = FileSystemUtils.ConcatenatePaths(path, FileSystemUtils.SanitizeFileName(fileName));
        }
        else
        {
            absoluteFilePath = FileSystemUtils.ConcatenatePaths(jobClusterDirectoryPath, FileSystemUtils.SanitizeFileName(fileName));
        }
        
        absoluteFilePath = FileSystemUtils.SanitizePath(absoluteFilePath);

        if (jobSpecification.Cluster.SchedulerType == SchedulerType.QScheduler && 
            !jobSpecification.Cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.Ssh) &&
            !jobSpecification.Cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.SshInteractive))
        {
            throw new NotSupportedException("File transfers are not supported for QScheduler clusters without SSH protocol.");
        }

        var fileManager = CreateFileSystemManager(jobSpecification);
        var succeeded = await fileManager.UploadFileToClusterByAbsolutePathAsync(fileStream, absoluteFilePath, jobSpecification.ClusterUser, jobSpecification.Cluster, 
                                                                         _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
        result.Add("Succeeded", succeeded);
        result.Add("Path", succeeded ? absoluteFilePath : null);
        return result;
    }

    public async Task<FileTransferMethod> ProvideCredentials(long modelProjectId, long modelClusterId, AdaptorUser loggedUser)
    {
        var project = _unitOfWork.ProjectRepository.GetById(modelProjectId);
        var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(modelClusterId, modelProjectId);
        
        if (clusterProject == null)
            throw new InvalidRequestException("NotExistingClusterProject", modelClusterId, modelProjectId);
        
        var cluster = clusterProject.Cluster;
        
        var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        var clusterUserAuthCredentials = await clusterLogic.GetNextAvailableUserCredentials(modelClusterId, modelProjectId, true, loggedUser.Id);

        if (clusterUserAuthCredentials == null)
            throw new ClusterAuthenticationException("NotExistingClusterAuthenticationCredentials", loggedUser.Id, modelClusterId);

        SignResponse response = new SignResponse();
        string publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(clusterUserAuthCredentials).PublicKeyInAuthorizedKeysFormat;

        if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            var resourceName = !string.IsNullOrEmpty(cluster.MasterNodeName)
                ? cluster.MasterNodeName
                : (cluster.FileTransferMethods.FirstOrDefault()?.ServerHostname ?? cluster.Name);

            response = await _sshCertificateAuthorityService
                .SignAsync(publicKey, _httpContextKeys.Context.SshCaToken, resourceName, _logger);
        }

        string username = (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication && SshCaSettings.UsePosixAccountFromCertificate) 
            ? response.PosixUsername 
            : clusterUserAuthCredentials.Username;

        var transferMethod = new FileTransferMethod
        {
            Protocol = cluster.FileTransferMethods.FirstOrDefault()!.Protocol,
            Port = cluster.FileTransferMethods.FirstOrDefault()!.Port ?? 22,
            Cluster = cluster,
            ServerHostname = cluster.FileTransferMethods.FirstOrDefault()?.ServerHostname,
            SharedBasePath = FileSystemUtils.ExpandRemotePath(clusterProject.ScratchStoragePath, username, null, cluster.CustomConfiguration),
            Credentials = new FileTransferKeyCredentials
            {
                Username = username,
                Password = clusterUserAuthCredentials.Password,
                FileTransferCipherType = clusterUserAuthCredentials.CipherType,
                CredentialsAuthType = clusterUserAuthCredentials.AuthenticationType,
                PrivateKey = clusterUserAuthCredentials.PrivateKey,
                PrivateKeyCertificate = (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication) 
                    ? response.SshCert 
                    : (string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKeyCertificate) ? null : clusterUserAuthCredentials.PrivateKeyCertificate),
                Passphrase = clusterUserAuthCredentials.PrivateKeyPassphrase,
                PublicKey = publicKey
            }
        };

        return transferMethod;
    }

    private async Task<byte[]> HandleDeletedJobFileDownloadAsync(
        SubmittedJobInfo jobInfo,
        string relativeFilePath,
        IRexFileSystemManager fileManager,
        AdaptorUser loggedUser)
    {
        _logger.LogInformation($"Getting file from archive for submitted job Id {jobInfo.Id} with user {loggedUser.GetLogIdentification()}");

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
                var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
                var localBasePath = Path.Combine(basePath, clusterConfig.InstanceIdentifierPath, clusterConfig.JobLogArchiveSubPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username);
 
                if (relativeFilePath.StartsWith(start1))
                {
                    try
                    {
                        var file = Path.Combine(localBasePath, relativeFilePath.TrimStart('/'));
                        return await fileManager.DownloadFileFromClusterByAbsolutePathAsync(jobInfo.Specification, file, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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
                        return await fileManager.DownloadFileFromClusterByAbsolutePathAsync(jobInfo.Specification, file, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
                    }
                }
            }
            return await fileManager.DownloadFileFromClusterAsync(jobInfo, relativeFilePath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
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

#pragma warning disable IDE1006
    private string _expirioToken
    {
        get => !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.IdpToken;
    }
#pragma warning restore IDE1006

    private static void VerifyOwner(SubmittedJobInfo jobInfo, AdaptorUser loggedUser)
    {
        if (jobInfo.Submitter.Id != loggedUser.Id)
        {
            throw new AdaptorUserNotAuthorizedForJobException("ClusterOperationRequiresOwner", loggedUser.GetLogIdentification(), jobInfo.Id);
        }
    }

    private static bool IsPathBlocked(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var normalized = path.Replace('\\', '/').Trim('/');
        return normalized.Equals(".heappe", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith(".heappe/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/.heappe/", StringComparison.OrdinalIgnoreCase);
    }

    private IRexFileSystemManager CreateFileSystemManager(JobSpecification jobSpecification)
    {
        var protocol = jobSpecification.FileTransferMethod.Protocol;
        var transferMethod = jobSpecification.FileTransferMethod;

        if (jobSpecification.Cluster.SchedulerType == SchedulerType.QScheduler && 
            (jobSpecification.Cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.Ssh) ||
             jobSpecification.Cluster.ConnectionProtocol.HasFlag(ClusterConnectionProtocol.SshInteractive)))
        {
            protocol = FileTransferProtocol.SftpScp;
            transferMethod = new FileTransferMethod
            {
                Id = jobSpecification.FileTransferMethod.Id,
                ServerHostname = jobSpecification.Cluster.MasterNodeName,
                Port = jobSpecification.Cluster.Port,
                Protocol = FileTransferProtocol.SftpScp,
                ClusterId = jobSpecification.Cluster.Id,
                Cluster = jobSpecification.Cluster,
                Credentials = jobSpecification.FileTransferMethod.Credentials,
                IsDeleted = false
            };
        }

        return FileSystemFactory.GetInstance(protocol, jobSpecification.Cluster)
            .CreateFileSystemManager(transferMethod, _sshCertificateAuthorityService, _logger);
    }

    #endregion
}
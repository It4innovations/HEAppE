using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using HEAppE.Utils;
using HEAppE.ExternalAuthentication;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

internal class ClusterInformationLogic : IClusterInformationLogic
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly ILogger _logger;
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IExpirioService _expirioService;
    private readonly ICredentialProvisioningLogic _credentialProvisioningLogic;

    internal ClusterInformationLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _logger = logger;
        _expirioService = expirioService;
        _credentialProvisioningLogic = LogicFactory.GetLogicFactory().CreateCredentialProvisioningLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys, expirioService, logger);
    }
    
    public IEnumerable<Cluster> ListAvailableClusters()
    {
        return _unitOfWork.ClusterRepository.GetAllWithActiveProjectFilter();
    }

    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsageAsync(long clusterNodeId, AdaptorUser loggedUser,
        long projectId)
    {
        var nodeType = GetClusterNodeTypeById(clusterNodeId)
            ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", clusterNodeId);

        var project = _unitOfWork.ProjectRepository.GetById(projectId)
            ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        var cluster = nodeType.Cluster
            ?? throw new InvalidRequestException("ClusterNodeNoReferenceToCluster", clusterNodeId);

        if (!nodeType.ClusterId.HasValue)
            throw new InvalidRequestException("ClusterNodeNoReferenceToClusterId", clusterNodeId);

        if (loggedUser?.Groups == null || !loggedUser.Groups.Any())
            throw new InvalidRequestException("UserHasNoGroups", loggedUser);

        var clusterProjectIds = cluster.ClusterProjects?
            .Where(x => x.ProjectId == projectId)
            .Select(y => y.ProjectId)
            .ToList() ?? new List<long>();

        var availableProjectIds = loggedUser.Groups
            .Where(g => g.ProjectId.HasValue && clusterProjectIds.Contains(g.ProjectId.Value))
            .Select(g => g.ProjectId!.Value)
            .Distinct()
            .ToList();

        if (availableProjectIds.Count == 0)
            throw new InvalidRequestException("UserNoAccessToClusterNode", loggedUser, clusterNodeId);

        var serviceAccount = await _unitOfWork.ClusterAuthenticationCredentialsRepository
            ?.GetServiceAccountCredentials(cluster.Id, projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, logger: _logger);

        if (serviceAccount is null)
            throw new InvalidRequestException("ProjectNoReferenceToCluster", projectId, cluster.Id);

        var schedulerFactory = SchedulerFactory.GetInstance(cluster.SchedulerType)
            ?? throw new InvalidOperationException("SchedulerFactoryInstanceIsNull");

        var scheduler = schedulerFactory.CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId:loggedUser.Id, _expirioService, _logger)
            ?? throw new InvalidOperationException("SchedulerInitializationFailed");

        return await scheduler.GetCurrentClusterNodeUsageAsync(nodeType, serviceAccount, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken);
    }

    public async Task<IEnumerable<string>> GetCommandTemplateParametersName(long commandTemplateId, long projectId,
        string userScriptPath, AdaptorUser loggedUser)
    {
        var commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId) ??
                              throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
        var project = _unitOfWork.ProjectRepository.GetById(projectId) ??
                      throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        if (commandTemplate.IsGeneric)
        {
            var scriptPath = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                .FirstOrDefault()?.Identifier;
            if (string.IsNullOrEmpty(scriptPath))
                throw new RequestedObjectDoesNotExistException("UserScriptNotDefined");

            if (string.IsNullOrEmpty(userScriptPath)) throw new InputValidationException("NoScriptPath");

            if (commandTemplate.ProjectId.HasValue)
                if (commandTemplate.ProjectId != projectId)
                    throw new RequestedObjectDoesNotExistException("CommandTemplateNotReferencedToProject",
                        commandTemplate.Id, projectId);
            var cluster = commandTemplate.ClusterNodeType.Cluster;
            var serviceAccountCredentials = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id,
                    projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id, logger: _logger);
            if (serviceAccountCredentials is null)
                throw new RequestedObjectDoesNotExistException("ServiceAccountCredentialsNotDefinedInCommandTemplate");

            var commandTemplateParameters = new List<string> { scriptPath };
            var scriptParams = (await SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id, _expirioService, _logger)
                .GetParametersFromGenericUserScriptAsync(cluster, serviceAccountCredentials, userScriptPath, _httpContextKeys.Context.SshCaToken, _httpContextKeys.Context.LEXISToken)).ToList();
            commandTemplateParameters.AddRange(scriptParams);
            return commandTemplateParameters;
        }

        return commandTemplate.TemplateParameters.Select(s => s.Identifier)
            .ToList();
    }
    
    
    public async Task<ClusterAuthenticationCredentials> InitializeCredentialInBackgroundTask(
        ClusterAuthenticationCredentials credential, long projectId, long? adaptorUserId)
    {
        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger);
        var status = await managementLogic.InitializeClusterScriptDirectory(
            projectId,
            true,
            adaptorUserId: adaptorUserId.HasValue ? adaptorUserId.Value : null,
            username: credential.Username, isAdministrator:true);
        _logger.LogInformation($"Initialized credential {credential.Username} for project {projectId} with status: {status}");
        return credential;
    }

    public async Task<ClusterAuthenticationCredentials> GetNextAvailableUserCredentials(long clusterId, long projectId,
        bool requireIsInitialized, long? adaptorUserId)
    {
        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
        if (cluster == null)
            throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);

        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        if (project == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        if (project.IsOneToOneMapping)
        {
            try
            {
                return await GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                    adaptorUserId.Value);
            }
            catch (RequestedObjectDoesNotExistException )
            {
                if (BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse)
                {
                    _logger.LogInformation($"Automatic initialization of cluster accounts is enabled. Attempting to initialize accounts for project {projectId} on cluster {clusterId} for adaptor user {adaptorUserId}");
                    await _credentialProvisioningLogic.InitializeClusterCredentials(clusterId, projectId, adaptorUserId, false);
                    return await GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                        adaptorUserId.Value);
                }
            }
        }
        
        IEnumerable<ClusterAuthenticationCredentials> credentials = new List<ClusterAuthenticationCredentials>();
        try
        {
            credentials = (await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                    clusterId, projectId, requireIsInitialized, null, _logger)).ToList();
        }
        catch(NotAllowedException ex)
        {
            if (BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse && ex.Message.Contains("ClusterAccountNotInitialized"))
            {
                credentials = (await _credentialProvisioningLogic.InitializeClusterCredentials(clusterId: clusterId, projectId: projectId, adaptorUserId: adaptorUserId, onlyServiceAccounts: false)).ToList();
            }
            else
            {
                throw;
            }
        }
        
        if (credentials == null || !credentials.Any())
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationWithoutCredentials", clusterId, projectId);

        var serviceCredentials = await
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, requireIsInitialized, null, _logger)
            ?? throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNoServiceAccount", clusterId, projectId);

        var firstCredentials = credentials.First();
        var lastUsedId = ClusterUserCache.GetLastUserId(cluster);
        
        if (lastUsedId is null)
        {
            ClusterUserCache.SetLastUserId(cluster, serviceCredentials, firstCredentials.Id);
            _logger.LogDebug("Using initial cluster account: {0}", firstCredentials.Username);
            return firstCredentials;
        }

        var creds = credentials.FirstOrDefault(account => account.Id > lastUsedId);
        creds ??= firstCredentials;

        ClusterUserCache.SetLastUserId(cluster, serviceCredentials, creds.Id);
        _logger.LogDebug("Using cluster account: {0}", creds.Username);        
        return creds;
    }
    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAndInitializeCredentials(long clusterId, 
                                                                                        long projectId, 
                                                                                        long adaptorUserId, 
                                                                                        bool requireIsInitialized, 
                                                                                        bool onlyServiceAccounts)
    {
        await SynchronizeCredentialsAsync(projectId, adaptorUserId);

        try
        {
            if (onlyServiceAccounts)
            {
                var serviceAccount = await _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetServiceAccountCredentials(clusterId, projectId, requireIsInitialized, adaptorUserId, _logger);

                return serviceAccount != null 
                    ? new List<ClusterAuthenticationCredentials> { serviceAccount } 
                    : Enumerable.Empty<ClusterAuthenticationCredentials>();
            }
            else
            {
                return (await _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetAuthenticationCredentialsForClusterAndProject(clusterId, projectId, requireIsInitialized, adaptorUserId, _logger)).ToList();
            }
        }
        catch (NotAllowedException ex)
        {
            bool isNotInitializedError = ex.Message != null && ex.Message.Contains("ClusterAccountNotInitialized");
            bool isAutoInitEnabled = BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse;

            if (isAutoInitEnabled && isNotInitializedError)
            {
                _logger.LogInformation("Auto-initializing credentials for ClusterId: {0}, ProjectId: {1}, ServiceAccount: {2}",
                    clusterId, projectId, onlyServiceAccounts);
                
                var credentials = await _credentialProvisioningLogic.InitializeClusterCredentials(
                    clusterId: clusterId, 
                    projectId: projectId, 
                    adaptorUserId: adaptorUserId, 
                    onlyServiceAccounts: onlyServiceAccounts);

                if (credentials.Any())
                {
                    return credentials;
                }

                // If initialization failed because they don't exist, try creating them
                return await _credentialProvisioningLogic.CreateAndInitializeMissingCredentials(
                    clusterId: clusterId,
                    projectId: projectId,
                    adaptorUserId: adaptorUserId);
            }
            throw;
        }
    }
    

    private async Task<ClusterAuthenticationCredentials> GetNextAvailableUserCredentialsByAdaptorUser(long clusterId, long projectId, bool requireIsInitialized, long adaptorUserId)
    {
        List<ClusterAuthenticationCredentials> credentials = (await GetAndInitializeCredentials(
            clusterId, projectId, adaptorUserId, requireIsInitialized, onlyServiceAccounts: false))
            .ToList(); 
        
        if (credentials.Count == 0)
        {
            if (BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse)
            {
                _logger.LogInformation("No credentials found for ClusterId: {0}, ProjectId: {1}, AdaptorUser: {2}. Attempting auto-creation.", clusterId, projectId, adaptorUserId);
                credentials = (await _credentialProvisioningLogic.CreateAndInitializeMissingCredentials(
                    clusterId: clusterId,
                    projectId: projectId,
                    adaptorUserId: adaptorUserId)).ToList();
            }

            if (credentials.Count == 0)
            {
                throw new RequestedObjectDoesNotExistException("FailedToRetrieveOrInitializeClusterAccount");
            }
        }

        ClusterAuthenticationCredentials serviceCredentials = (await GetAndInitializeCredentials(
            clusterId, projectId, adaptorUserId, requireIsInitialized, onlyServiceAccounts: true))
            .FirstOrDefault();

        if (serviceCredentials == null)
        {
            throw new RequestedObjectDoesNotExistException("FailedToRetrieveOrInitializeClusterAccount");
        }
        
        var firstCredentials = credentials[0];
        var lastUsedId = AdaptorUserProjectClusterUserCache.GetLastUserId(adaptorUserId, projectId, clusterId);

        ClusterAuthenticationCredentials creds;
        if (lastUsedId == null)
        {
            creds = firstCredentials;
        }
        else
        {
            creds = credentials.FirstOrDefault(account => account.Id > lastUsedId);
            creds ??= firstCredentials;
        }

        AdaptorUserProjectClusterUserCache.SetLastUserId(
            adaptorUserId, projectId, clusterId, serviceCredentials.Id, creds.Id);
        
        _logger.LogDebug("Using cluster account: {0}", creds.Username);

        return creds;
    }

    public ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId)
    {
        var nodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(clusterNodeTypeId);

        if (nodeType == null)
            throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", clusterNodeTypeId);

        return nodeType;
    }

    public Cluster GetClusterById(long clusterId)
    {
        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
        return cluster == null
            ? throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId)
            : cluster;
    }

    public IEnumerable<ClusterNodeType> ListClusterNodeTypes()
    {
        return _unitOfWork.ClusterNodeTypeRepository.GetAllWithPossibleCommands();
    }

    public bool IsUserAvailableToRun(ClusterAuthenticationCredentials user)
    {
        var allRunningJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished().ToList();
        var userRunningJobs = allRunningJobs.Where(w =>
            w.Specification.ClusterUser == user && w.State > JobState.Configuring && w.State <= JobState.Running);

        return !userRunningJobs.Any();
    }

    private async Task<string?> ResolveUsernameFromContextAsync(long? adaptorUserId, Project? project = null)
    {
        string? username = null;
        
        // 1. SSH CA resolution
        if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(_httpContextKeys.Context.SshCaToken))
        {
            try {
                username = await _sshCertificateAuthorityService.GetPosixUsernameAsync(_httpContextKeys.Context.SshCaToken, _logger);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "SSH CA username resolution failed.");
            }
        }
        
        // 2. Kerberos enriched username resolution
        if (string.IsNullOrEmpty(username))
        {
            var token = !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.FIPToken;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    username = await _expirioService.GetEnrichedUsernameAsync(token, _logger);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Kerberos enriched username resolution failed, falling back to JWT.");
                }
            }
        }
        
        // 3. Token preferred_username resolution
        if (string.IsNullOrEmpty(username))
        {
            var token = !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken) ? _httpContextKeys.Context.LEXISToken : _httpContextKeys.Context.FIPToken;
            if (!string.IsNullOrEmpty(token))
            {
                try 
                {
                    var decoded = JwtTokenDecoder.Decode(token);
                    if (!string.IsNullOrEmpty(decoded.PreferedUsername)) {
                        username = decoded.PreferedUsername;
                    } else if (project != null) {
                        username = StringUtils.GenerateUsername(adaptorUserId ?? 0, project.AccountingString);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decode JWT token for username resolution.");
                }
            }
        }
        
        return username;
    }

    private async Task SynchronizeCredentialsAsync(long projectId, long adaptorUserId)
    {
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        var username = await ResolveUsernameFromContextAsync(adaptorUserId, project);
        
        if (string.IsNullOrEmpty(username)) return;

        var existingForUser = await _unitOfWork.ClusterAuthenticationCredentialsRepository
            .GetAuthenticationCredentialsProject(projectId, requireIsInitialized: false, adaptorUserId: adaptorUserId, logger: _logger);

        bool anyChanged = false;
        foreach (var cred in existingForUser)
        {
            if (cred.Username != username)
            {
                _logger.LogInformation($"Synchronizing username for Credential ID {cred.Id}: {cred.Username} -> {username}");
                cred.Username = username;
                await _unitOfWork.ClusterAuthenticationCredentialsRepository.UpdateAsync(cred);
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            await _unitOfWork.SaveAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using log4net;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

internal class ClusterInformationLogic : IClusterInformationLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    internal ClusterInformationLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #endregion

    #region Instance

    /// <summary>
    ///     Unit of work
    /// </summary>
    protected readonly IUnitOfWork _unitOfWork;

    /// <summary>
    ///     Log instance
    /// </summary>
    protected readonly ILog _log;
    
    /// <summary>
    /// SSH CA service
    /// </summary>
    protected readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;

    /// <summary>
    /// HTTP context keys
    /// </summary>
    private readonly IHttpContextKeys _httpContextKeys;

    #endregion

    #region IClusterInformationLogic

    public IEnumerable<Cluster> ListAvailableClusters()
    {
        return _unitOfWork.ClusterRepository.GetAllWithActiveProjectFilter();
    }

    public async Task<ClusterNodeUsage> GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser,
        long projectId)
    {
        var nodeType = GetClusterNodeTypeById(clusterNodeId)
            ?? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotFound", clusterNodeId);

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
            ?.GetServiceAccountCredentials(cluster.Id, projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);

        if (serviceAccount is null)
            throw new InvalidRequestException("ProjectNoReferenceToCluster", projectId, cluster.Id);

        var schedulerFactory = SchedulerFactory.GetInstance(cluster.SchedulerType)
            ?? throw new InvalidOperationException("SchedulerFactoryInstanceIsNull");

        var scheduler = schedulerFactory.CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId:loggedUser.Id)
            ?? throw new InvalidOperationException("SchedulerInitializationFailed");

        return scheduler.GetCurrentClusterNodeUsage(nodeType, serviceAccount, _httpContextKeys.Context.SshCaToken);
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
                    projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
            if (serviceAccountCredentials is null)
                throw new RequestedObjectDoesNotExistException("ServiceAccountCredentialsNotDefinedInCommandTemplate");

            var commandTemplateParameters = new List<string> { scriptPath };
            commandTemplateParameters.AddRange(SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, project, _sshCertificateAuthorityService, adaptorUserId: loggedUser.Id)
                .GetParametersFromGenericUserScript(cluster, serviceAccountCredentials, userScriptPath, _httpContextKeys.Context.SshCaToken).ToList());
            return commandTemplateParameters;
        }

        return commandTemplate.TemplateParameters.Select(s => s.Identifier)
            .ToList();
    }
    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> InitializeCredentials(long projectId, long clusterId, long? adaptorUserId)
    {
        var credentials = await
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                clusterId, projectId, false, null);
        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        foreach (var credential in credentials)
        {
            var status = managementLogic.InitializeClusterScriptDirectory(
                projectId, 
                true,
                adaptorUserId: adaptorUserId.HasValue ? adaptorUserId.Value : null,
                username: credential.Username
            );
            _log.Info($"Initialized credential {credential.Username} for project {projectId} with status: {status}");
        }
        return credentials;
    }
    
    public ClusterAuthenticationCredentials InitializeCredential(ClusterAuthenticationCredentials credential, long projectId, long? adaptorUserId)
    {
        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        var status = managementLogic.InitializeClusterScriptDirectory(
            projectId,
            true,
            adaptorUserId: adaptorUserId.HasValue ? adaptorUserId.Value : null,
            username: credential.Username);
        _log.Info($"Initialized credential {credential.Username} for project {projectId} with status: {status}");
        return credential;
    }

    public async Task<ClusterAuthenticationCredentials> GetNextAvailableUserCredentials(long clusterId, long projectId,
        bool requireIsInitialized, long? adaptorUserId)
    {
        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
        if (cluster == null)
            throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);

        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        _unitOfWork.ProjectRepository.Detach(project);
        if (project == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotExists", projectId);

        if (project.IsOneToOneMapping)
        {
            try
            {
                return await GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                    adaptorUserId.Value);
            }
            catch (NotAllowedException ex)
            {
                if (BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse)
                {
                    _log.Info($"Automatic initialization of cluster accounts is enabled. Attempting to initialize accounts for project {projectId} on cluster {clusterId} for adaptor user {adaptorUserId}");
                    var initializedCredentials = InitializeCredentials(projectId, clusterId, adaptorUserId);
                    return await GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                        adaptorUserId.Value);
                }
            }
        }
        
        //return all non service account for specific cluster and project
        IEnumerable<ClusterAuthenticationCredentials> credentials =  new List<ClusterAuthenticationCredentials>();
        try
        {
            credentials = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                    clusterId, projectId, requireIsInitialized, null);
        }
        catch(NotAllowedException ex)
        {
            //if initialize not initialized credentials boolean is true
            if (BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse && ex.Message.Contains("ClusterAccountNotInitialized"))
            {
                credentials = await InitializeClusterCredentials(clusterId: clusterId, projectId: projectId, adaptorUserId: adaptorUserId, onlyServiceAccounts: false);
            }
            else
            {
                throw;
            }
        }
        
        
        if (credentials == null || credentials?.Count() == 0)
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", clusterId, projectId);

        var serviceCredentials = await
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, requireIsInitialized, null)
            ?? throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNoServiceAccount", clusterId, projectId);

        var firstCredentials = credentials.FirstOrDefault();

        var lastUsedId = ClusterUserCache.GetLastUserId(cluster);
        if (lastUsedId is null)
        {
            // No user has been used from this cluster
            // return first usable account
            ClusterUserCache.SetLastUserId(cluster, serviceCredentials, firstCredentials.Id);
            _log.DebugFormat("Using initial cluster account: {0}", firstCredentials.Username);
            return firstCredentials;
        }

        // Return first user with Id higher than the last one
        var creds = (from account in credentials where account.Id > lastUsedId select account).FirstOrDefault();
        // No credentials with Id higher than last used found
        // use first usable account
        creds ??= firstCredentials;

        ClusterUserCache.SetLastUserId(cluster, serviceCredentials, creds.Id);
        _log.DebugFormat("Using cluster account: {0}", creds.Username);
        return creds;
    }
    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAndInitializeCredentials(long clusterId, 
                                                                                        long projectId, 
                                                                                        long adaptorUserId, 
                                                                                        bool requireIsInitialized, 
                                                                                        bool onlyServiceAccounts)
    {
        try
        {
            if (onlyServiceAccounts)
            {
                // Získání servisního účtu (vrací jeden objekt nebo null)
                var serviceAccount = await _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetServiceAccountCredentials(clusterId, projectId, requireIsInitialized, adaptorUserId);

                // Musíme zabalit jeden objekt do IEnumerable, aby seděl návratový typ
                return serviceAccount != null 
                    ? new List<ClusterAuthenticationCredentials> { serviceAccount } 
                    : Enumerable.Empty<ClusterAuthenticationCredentials>();
            }
            else
            {
                // Získání uživatelských účtů (již vrací IEnumerable)
                return await _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetAuthenticationCredentialsForClusterAndProject(clusterId, projectId, requireIsInitialized, adaptorUserId);
            }
        }
        catch (NotAllowedException ex)
        {
            // Kontrola: Je chyba způsobena tím, že účet není inicializován? A je povolena auto-inicializace?
            bool isNotInitializedError = ex.Message != null && ex.Message.Contains("ClusterAccountNotInitialized");
            bool isAutoInitEnabled = BusinessLogicConfiguration.AutoInitializeProjectCredentialsOnFirstUse;

            if (isAutoInitEnabled && isNotInitializedError)
            {
                _log.InfoFormat("Auto-initializing credentials for ClusterId: {0}, ProjectId: {1}, ServiceAccount: {2}", 
                    clusterId, projectId, onlyServiceAccounts);

                // Volání existující metody pro inicializaci
                // Předpokládám, že InitializeClusterCredentials vrací kolekci (v původním kódu .FirstOrDefault() naznačoval kolekci)
                return await InitializeClusterCredentials(
                    clusterId: clusterId, 
                    projectId: projectId, 
                    adaptorUserId: adaptorUserId, 
                    onlyServiceAccounts: onlyServiceAccounts);
            }

            // Pokud to není chyba inicializace nebo je auto-init vypnutý, vyhoď výjimku dál
            throw;
        }
    }
    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> CreateAndInitializeMissingCredentials(
        long clusterId, 
        long projectId, 
        long adaptorUserId)
    {
        _log.InfoFormat("Creating missing credentials for ClusterId: {0}, ProjectId: {1}", clusterId, projectId);

        //invoke management logic and run CreateSecureShellKey
        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        managementLogic.CreateSecureShellKey(
            credentials: new List<(string, string)> { ($"account_{projectId}_{adaptorUserId}", string.Empty) },
            projectId: projectId,
            adaptorUserId: adaptorUserId);
        
        return await InitializeClusterCredentials(
            clusterId: clusterId, 
            projectId: projectId, 
            adaptorUserId: adaptorUserId, 
            onlyServiceAccounts: false); 
    }

    private async Task<ClusterAuthenticationCredentials> GetNextAvailableUserCredentialsByAdaptorUser(long clusterId, long projectId, bool requireIsInitialized, long adaptorUserId)
    {
        List<ClusterAuthenticationCredentials> credentials = (await GetAndInitializeCredentials(
            clusterId, projectId, adaptorUserId, requireIsInitialized, onlyServiceAccounts: false))
            .ToList(); 

        // 2. NOVÝ POŽADAVEK: Auto-vytvoření, pokud credentials neexistují
        if (credentials.Count == 0 && SshCaSettings.UseCertificateAuthorityForAuthentication)
        {
            // Předpoklad: Tato metoda vytvoří v DB jak User, tak Service účty a vrátí User účty.
            var newCredentials = await CreateAndInitializeMissingCredentials(clusterId, projectId, adaptorUserId);
            credentials = newCredentials.ToList(); // Opět ujistit se, že máme List
        }

        // 3. Kontrola, zda ne-servisní účty existují
        if (credentials.Count == 0)
        {
            throw new RequestedObjectDoesNotExistException("FailedToRetrieveOrInitializeClusterAccount");
        }

        // 4. Získání servisního účtu
        // Zde není nutné ToList(), protože voláme jen FirstOrDefault()
        ClusterAuthenticationCredentials serviceCredentials = (await GetAndInitializeCredentials(
            clusterId, projectId, adaptorUserId, requireIsInitialized, onlyServiceAccounts: true))
            .FirstOrDefault();

        if (serviceCredentials == null)
        {
            // if service account is missing, something is seriously wrong
            throw new RequestedObjectDoesNotExistException("FailedToRetrieveOrInitializeClusterAccount");
        }
        
        // 5. Rotation logic
        var firstCredentials = credentials[0];
        var lastUsedId = AdaptorUserProjectClusterUserCache.GetLastUserId(adaptorUserId, projectId, clusterId);

        ClusterAuthenticationCredentials creds;
        if (lastUsedId == null)
        {
            creds = firstCredentials;
        }
        else
        {
            // Memory efficient way to get next account
            creds = credentials.FirstOrDefault(account => account.Id > lastUsedId);
            creds ??= firstCredentials;
        }

        AdaptorUserProjectClusterUserCache.SetLastUserId(
            adaptorUserId, projectId, clusterId, serviceCredentials.Id, creds.Id);
        
        _log.DebugFormat("Using cluster account: {0}", creds.Username);

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
        var allRunningJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished();
        var userRunningJobs = allRunningJobs.Where(w =>
            w.Specification.ClusterUser == user && w.State > JobState.Configuring && w.State <= JobState.Running);

        return !userRunningJobs.Any();
    }

    #endregion
    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> InitializeClusterCredentials(
        long clusterId,
        long projectId,
        long? adaptorUserId, bool onlyServiceAccounts)
    {
        var initializedCredentials = new List<ClusterAuthenticationCredentials>();
        IEnumerable<ClusterAuthenticationCredentials> notInitializedCredentials = new List<ClusterAuthenticationCredentials>();
        if (onlyServiceAccounts)
        {
            var serviceAccount = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, false, adaptorUserId)
                ?? throw new RequestedObjectDoesNotExistException("ClusterAuthenticationCredentialsNoServiceAccount", clusterId, projectId, adaptorUserId);

            notInitializedCredentials = new List<ClusterAuthenticationCredentials> { serviceAccount };
        }
        else
        {
            notInitializedCredentials = await
                _unitOfWork.ClusterAuthenticationCredentialsRepository
                    .GetAuthenticationCredentialsForClusterAndProject(
                        clusterId, projectId, false, adaptorUserId);
        }
       

        foreach (var credential in notInitializedCredentials)
        {
            var clusterProjectCredential = credential.ClusterProjectCredentials.FirstOrDefault(cpc =>
                cpc.ClusterProject.ProjectId == projectId && cpc.ClusterProject.ClusterId == clusterId);

            if (clusterProjectCredential == null)
            {
                throw new RequestedObjectDoesNotExistException(
                    "ClusterProjectCombinationNotFound", clusterId, projectId);
            }

            var initProject = clusterProjectCredential.ClusterProject.Project;
            var initCluster = clusterProjectCredential.ClusterProject.Cluster;
            var localBasepath = clusterProjectCredential.ClusterProject.ScratchStoragePath;

            var scheduler = SchedulerFactory
                .GetInstance(initCluster.SchedulerType)
                .CreateScheduler(initCluster, initProject, _sshCertificateAuthorityService, adaptorUserId);

            string path = Path.Combine(initProject.AccountingString,
                HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath);
            var isInitialized = scheduler.InitializeClusterScriptDirectory(
                path,
                true,
                localBasepath,
                initCluster,
                credential,
                clusterProjectCredential.IsServiceAccount,
                _httpContextKeys.Context.SshCaToken);

            if (isInitialized)
            {
                clusterProjectCredential.IsInitialized = true;
                _unitOfWork.Save();
                initializedCredentials.Add(credential);
            }
        }

        return initializedCredentials;
    }

}
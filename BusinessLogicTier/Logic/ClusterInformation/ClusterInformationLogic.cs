using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.Exceptions.External;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using log4net;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

internal class ClusterInformationLogic : IClusterInformationLogic
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="unitOfWork">Unit of work</param>
    internal ClusterInformationLogic(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

    #endregion

    #region IClusterInformationLogic

    public IEnumerable<Cluster> ListAvailableClusters()
    {
        return _unitOfWork.ClusterRepository.GetAllWithActiveProjectFilter();
    }

    public ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser, long projectId)
    {
        
        var nodeType = GetClusterNodeTypeById(clusterNodeId);
        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        if (!nodeType.ClusterId.HasValue) throw new InvalidRequestException("ClusterNodeNoReferenceToCluster");

        if (project is null) throw new RequestedObjectDoesNotExistException("ProjectNotFound");

        var clusterProjectIds = nodeType.Cluster.ClusterProjects.Where(x => x.ProjectId == projectId)
            .Select(y => y.ProjectId);
        var availableProjectIds = loggedUser.Groups.Where(g => clusterProjectIds.Contains(g.ProjectId.Value))
            .Select(x => x.ProjectId.Value).Distinct().ToList();
        if (availableProjectIds.Count == 0)
            throw new InvalidRequestException("UserNoAccessToClusterNode", loggedUser, clusterNodeId);
        var serviceAccount =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                nodeType.ClusterId.Value, projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
        return serviceAccount is null
            ? throw new InvalidRequestException("ProjectNoReferenceToCluster", projectId, nodeType.ClusterId.Value)
            : SchedulerFactory.GetInstance(nodeType.Cluster.SchedulerType)
                .CreateScheduler(nodeType.Cluster, project, adaptorUserId:loggedUser.Id)
                .GetCurrentClusterNodeUsage(nodeType, serviceAccount);
    }

    public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId,
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
            var serviceAccountCredentials =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id,
                    projectId, requireIsInitialized: true, adaptorUserId: loggedUser.Id);
            if (serviceAccountCredentials is null)
                throw new RequestedObjectDoesNotExistException("ServiceAccountCredentialsNotDefinedInCommandTemplate");

            var commandTemplateParameters = new List<string> { scriptPath };
            commandTemplateParameters.AddRange(SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, project, adaptorUserId: loggedUser.Id)
                .GetParametersFromGenericUserScript(cluster, serviceAccountCredentials, userScriptPath).ToList());
            return commandTemplateParameters;
        }

        return commandTemplate.TemplateParameters.Select(s => s.Identifier)
            .ToList();
    }
    
    private IEnumerable<ClusterAuthenticationCredentials> InitializeCredentials(long projectId, long clusterId, long? adaptorUserId)
    {
        var credentials =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                clusterId, projectId, false, null);
        var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(_unitOfWork);
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

    public ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
        if (cluster == null)
            throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);

        var project = _unitOfWork.ProjectRepository.GetById(projectId);
        if (project == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotExists", projectId);

        if (project.IsOneToOneMapping)
        {
            try
            {
                return GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                    adaptorUserId.Value);
            }
            catch (NotAllowedException ex)
            {
                if (BusinessLogicConfiguration.AutomaticClusterAccountInitialization)
                {
                    _log.Info($"Automatic initialization of cluster accounts is enabled. Attempting to initialize accounts for project {projectId} on cluster {clusterId} for adaptor user {adaptorUserId}");
                    var initializedCredentials = InitializeCredentials(projectId, clusterId, adaptorUserId);
                    return GetNextAvailableUserCredentialsByAdaptorUser(clusterId, projectId, requireIsInitialized,
                        adaptorUserId.Value);
                }
            }
        }
        
        //return all non service account for specific cluster and project
        IEnumerable<ClusterAuthenticationCredentials> credentials = new List<ClusterAuthenticationCredentials>();
        try
        {
            credentials =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                    clusterId, projectId, requireIsInitialized, null);
        }
        catch (NotAllowedException ex)
        {
            if (BusinessLogicConfiguration.AutomaticClusterAccountInitialization)
            {
                _log.Info($"Automatic initialization of cluster accounts is enabled. Attempting to initialize accounts for project {projectId} on cluster {clusterId}");
                credentials = InitializeCredentials(projectId, clusterId, null);
            }
        }

        if (credentials == null || credentials?.Count() == 0)
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", clusterId, projectId);

        var serviceCredentials =
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

    private ClusterAuthenticationCredentials GetNextAvailableUserCredentialsByAdaptorUser(long clusterId, long projectId, bool requireIsInitialized, long adaptorUserId)
    {
        //return all non service account for specific cluster and project
        var credentials =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(
                clusterId, projectId, requireIsInitialized, adaptorUserId);
        if (credentials == null || credentials?.Count() == 0)
            throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", clusterId, projectId);

        var serviceCredentials =
            _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId, requireIsInitialized, adaptorUserId)
            ?? throw new RequestedObjectDoesNotExistException("ClusterAuthenticationCredentialsNoServiceAccount", clusterId, projectId, adaptorUserId);
            
        var firstCredentials = credentials.FirstOrDefault();
        var lastUsedId = AdaptorUserProjectClusterUserCache.GetLastUserId(adaptorUserId, projectId, clusterId);
        if (lastUsedId is null)
        {
            // No user has been used from this cluster
            // return first usable account
            AdaptorUserProjectClusterUserCache.SetLastUserId(adaptorUserId, projectId, clusterId, serviceCredentials.Id, firstCredentials.Id);
            _log.DebugFormat("Using initial cluster account: {0}", firstCredentials.Username);
            return firstCredentials;
        }

        // Return first user with Id higher than the last one
        var creds = (from account in credentials where account.Id > lastUsedId select account).FirstOrDefault();
        // No credentials with Id higher than last used found
        // use first usable account
        creds ??= firstCredentials;

        AdaptorUserProjectClusterUserCache.SetLastUserId(adaptorUserId, projectId, clusterId, serviceCredentials.Id, creds.Id);
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
}
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation
{
    internal class ClusterInformationLogic : IClusterInformationLogic
    {
        #region Instance
        /// <summary>
        /// Unit of work
        /// </summary>
        protected readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Log instance
        /// </summary>
        protected readonly ILog _log;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        internal ClusterInformationLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region IClusterInformationLogic

        public IEnumerable<Cluster> ListAvailableClusters()
        {
            return _unitOfWork.ClusterRepository.GetAllWithActiveProjectFilter();
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser, long projectId)
        {
            ClusterNodeType nodeType = GetClusterNodeTypeById(clusterNodeId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (!nodeType.ClusterId.HasValue)
            {
                throw new InvalidRequestException("The specified cluster node has no reference to cluster.");
            }

            if (project is null || !project.IsDeleted)
            {
                throw new InvalidRequestException("The specified project does not exist in the system.");
            }

            var clusterProjectIds = nodeType.Cluster.ClusterProjects.Where(x => x.ProjectId == projectId).Select(y=>y.ProjectId);
            var availableProjectIds = loggedUser.Groups.Where(g => clusterProjectIds.Contains(g.ProjectId.Value)).Select(x => x.ProjectId.Value).Distinct().ToList();
            if (availableProjectIds.Count() == 0)
            {
                throw new InvalidRequestException($"User {loggedUser} has no access to ClusterNodeId {clusterNodeId}.");
            }
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(nodeType.ClusterId.Value, projectId);
            if (serviceAccount is null)
            {
                throw new InvalidRequestException($"Project {projectId} has no refrence to Cluster {nodeType.ClusterId.Value}");
            }
            return SchedulerFactory.GetInstance(nodeType.Cluster.SchedulerType).CreateScheduler(nodeType.Cluster, project)
                                    .GetCurrentClusterNodeUsage(nodeType, serviceAccount);

        }

        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, AdaptorUser loggedUser)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (project is null || !project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException($"The specified project with ID '{projectId}' is not defined in HEAppE!");
            }

            if (commandTemplate.IsGeneric)
            {
                string scriptPath = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                        .FirstOrDefault()?.Identifier;
                if (string.IsNullOrEmpty(scriptPath))
                {
                    throw new RequestedObjectDoesNotExistException("The user-script command parameter for the generic command template is not defined in HEAppE!");
                }

                if (string.IsNullOrEmpty(userScriptPath))
                {
                    throw new RequestedObjectDoesNotExistException("The generic command template should contain script path!");
                }

                if (commandTemplate.ProjectId.HasValue)
                {
                    if (commandTemplate.ProjectId != projectId)
                    {
                        throw new RequestedObjectDoesNotExistException($"Specified CommandTemplateId \"{commandTemplate.Id}\" is not referenced to ProjectId \"{projectId}\".");
                    }
                }
                Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
                var serviceAccountCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
                if (serviceAccountCredentials is null)
                {
                    throw new RequestedObjectDoesNotExistException("ServiceAccountCredentials is not defined in the system for this CommandTemplate.");
                }

                var commandTemplateParameters = new List<string>() { scriptPath };
                commandTemplateParameters.AddRange(SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project).GetParametersFromGenericUserScript(cluster, serviceAccountCredentials, userScriptPath).ToList());
                return commandTemplateParameters;
            }
            else
            {
                return commandTemplate.TemplateParameters.Select(s => s.Identifier)
                                                          .ToList();
            }
        }

        public ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId, long projectId)
        {
            Cluster cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
            if (cluster == null)
            {
                _log.Error("Requested cluster with Id=" + clusterId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster with Id=" + clusterId + " does not exist in the system.");
            }

            //return all non service acccount for specific cluster and project
            var credentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(clusterId, projectId);
            if (credentials == null || credentials?.Count() == 0)
            {
                _log.Error($"Requested combination for cluster Id={clusterId} with Project Id={projectId} has no reference in the system.");
                throw new RequestedObjectDoesNotExistException($"Requested combination for cluster Id={clusterId} with Project Id={projectId} has no reference in the system.");
            }
            var serviceCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId);
            if (serviceCredentials == null)
            {
                _log.Error($"Requested combination for cluster Id={clusterId} with Project Id={projectId} has no Service Account Credentials reference in the system.");
                throw new RequestedObjectDoesNotExistException($"Requested combination for cluster Id={clusterId} with Project Id={projectId} has no Service Account Credentials reference in the system.");
            }
            var firstCredentials = credentials.FirstOrDefault();

            var lastUsedId = ClusterUserCache.GetLastUserId(cluster);
            if (lastUsedId is null)
            {   // No user has been used from this cluster
                // return first usable account
                ClusterUserCache.SetLastUserId(cluster, serviceCredentials, firstCredentials.Id);
                _log.DebugFormat("Using initial cluster account: {0}", firstCredentials.Username);
                return firstCredentials;
            }
            else
            {
                // Return first user with Id higher than the last one
                ClusterAuthenticationCredentials creds = (from account in credentials where account.Id > lastUsedId select account).FirstOrDefault();
                if (creds == null)
                {
                    // No credentials with Id higher than last used found
                    // use first usable account
                    creds = firstCredentials;
                }

                ClusterUserCache.SetLastUserId(cluster, serviceCredentials, firstCredentials.Id);
                _log.DebugFormat("Using cluster account: {0}", creds.Username);
                return creds;
            }
        }

        public ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId)
        {
            ClusterNodeType nodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(clusterNodeTypeId);

            if (nodeType == null)
            {
                _log.Error("Requested cluster node type with Id=" + clusterNodeTypeId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster node type with Id=" + clusterNodeTypeId + " does not exist in the system.");
            }

            return nodeType;
        }

        public Cluster GetClusterById(long clusterId)
        {
            Cluster cluster = _unitOfWork.ClusterRepository.GetById(clusterId);

            if (cluster == null)
            {
                _log.Error("Requested cluster with Id=" + clusterId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster with Id=" + clusterId + " does not exist in the system.");
            }

            return cluster;
        }

        public IEnumerable<ClusterNodeType> ListClusterNodeTypes()
        {
            return _unitOfWork.ClusterNodeTypeRepository.GetAllWithPossibleCommands();
        }

        public bool IsUserAvailableToRun(ClusterAuthenticationCredentials user)
        {
            IEnumerable<SubmittedJobInfo> allRunningJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished();
            var userRunningJobs = allRunningJobs.Where(w => w.Specification.ClusterUser == user && w.State > JobState.Configuring && w.State <= JobState.Running);

            return !userRunningJobs.Any();
        }
        #endregion
    }
}
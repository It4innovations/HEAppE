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
            return _unitOfWork.ClusterRepository.GetAll();
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, long projectId, AdaptorUser loggedUser)
        {
            ClusterNodeType nodeType = GetClusterNodeTypeById(clusterNodeId);
            if (!nodeType.ClusterId.HasValue)
            {
                throw new InvalidRequestException("The specified cluster node has no reference to cluster.");
            }
            var serviceCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll()
                                            .Where(c => c.ClusterProjectCredentials.Contains(
                                                new ClusterProjectCredentials()
                                                {
                                                    ClusterId = nodeType.ClusterId.Value,
                                                    ProjectId = projectId,
                                                    IsServiceAccount = true
                                                })).FirstOrDefault();
            return SchedulerFactory.GetInstance(nodeType.Cluster.SchedulerType).CreateScheduler(nodeType.Cluster)
                                    .GetCurrentClusterNodeUsage(nodeType, serviceCredentials);

        }

        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, AdaptorUser loggedUser)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
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

                Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
                var serviceAccountCredentials = commandTemplate.Project.ClusterProjects.Find(cp=>
                        cp.ClusterId == cluster.Id 
                        && cp.ProjectId == projectId)?
                            .ClusterProjectCredentials.Find(cpc=>
                            cpc.ClusterId == cluster.Id &&
                            cpc.ProjectId == projectId &&
                            cpc.IsServiceAccount)?
                            .ClusterAuthenticationCredentials;

                var commandTemplateParameters = new List<string>() { scriptPath };
                commandTemplateParameters.AddRange(SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetParametersFromGenericUserScript(cluster, serviceAccountCredentials, userScriptPath).ToList());
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
            var credentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll()
                                            .Where(c=>c.ClusterProjectCredentials.Contains(
                                                new ClusterProjectCredentials()
                                                {
                                                    ClusterId = clusterId,
                                                    ProjectId = projectId,
                                                    IsServiceAccount = false
                                                })).ToList();


            var lastUsedId = ClusterUserCache.GetLastUserId(cluster);
            if (lastUsedId is null)
            {   // No user has been used from this cluster
                // return first usable account
                ClusterUserCache.SetLastUserId(cluster, credentials[0].Id, projectId);
                _log.DebugFormat("Using initial cluster account: {0}", credentials[0].Username);
                return credentials[0];
            }
            else
            {
                // Return first user with Id higher than the last one
                ClusterAuthenticationCredentials creds = (from account in credentials where account.Id > lastUsedId select account).FirstOrDefault();
                if (creds == null)
                {
                    // No credentials with Id higher than last used found
                    // use first usable account
                    creds = credentials[0];
                }

                ClusterUserCache.SetLastUserId(cluster, creds.Id, projectId);
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
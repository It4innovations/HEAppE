﻿using HEAppE.Exceptions.External;
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
                throw new InvalidRequestException("ClusterNodeNoReferenceToCluster");
            }

            if (project is null || project.IsDeleted)
            {
                throw new InvalidRequestException("The specified project does not exist in the system.");
            }

            var clusterProjectIds = nodeType.Cluster.ClusterProjects.Where(x => x.ProjectId == projectId).Select(y=>y.ProjectId);
            var availableProjectIds = loggedUser.Groups.Where(g => clusterProjectIds.Contains(g.ProjectId.Value)).Select(x => x.ProjectId.Value).Distinct().ToList();
            if (availableProjectIds.Count == 0)
            {
                throw new InvalidRequestException("UserNoAccessToClusterNode", loggedUser, clusterNodeId);
            }
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(nodeType.ClusterId.Value, projectId);
            return serviceAccount is null
                ? throw new InvalidRequestException("ProjectNoReferenceToCluster", projectId, nodeType.ClusterId.Value)
                : SchedulerFactory.GetInstance(nodeType.Cluster.SchedulerType).CreateScheduler(nodeType.Cluster, project)
                                    .GetCurrentClusterNodeUsage(nodeType, serviceAccount);

        }

        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, long projectId, string userScriptPath, AdaptorUser loggedUser)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            if (project is null && project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }

            if (commandTemplate.IsGeneric)
            {
                string scriptPath = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                        .FirstOrDefault()?.Identifier;
                if (string.IsNullOrEmpty(scriptPath))
                {
                    throw new RequestedObjectDoesNotExistException("UserScriptNotDefined");
                }

                if (string.IsNullOrEmpty(userScriptPath))
                {
                    throw new InputValidationException("NoScriptPath");
                }

                if (commandTemplate.ProjectId.HasValue)
                {
                    if (commandTemplate.ProjectId != projectId)
                    {
                        throw new RequestedObjectDoesNotExistException("CommandTemplateNotReferencedToProject", commandTemplate.Id, projectId);
                    }
                }
                Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
                var serviceAccountCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
                if (serviceAccountCredentials is null)
                {
                    throw new RequestedObjectDoesNotExistException("ServiceAccountCredentialsNotDefinedInCommandTemplate");
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
            Cluster cluster = _unitOfWork.ClusterRepository.GetById(clusterId) ?? throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);

            //return all non service account for specific cluster and project
            var credentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForClusterAndProject(clusterId, projectId);
            if (credentials == null || credentials?.Count() == 0)
            {
                throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", clusterId, projectId);
            }
            var serviceCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(clusterId, projectId)
                ?? throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNoServiceAccount", clusterId, projectId);

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
                // No credentials with Id higher than last used found
                // use first usable account
                creds ??= firstCredentials;

                ClusterUserCache.SetLastUserId(cluster, serviceCredentials, creds.Id);
                _log.DebugFormat("Using cluster account: {0}", creds.Username);
                return creds;
            }
        }

        public ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId)
        {
            ClusterNodeType nodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(clusterNodeTypeId);
            return nodeType == null ? throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists", clusterNodeTypeId) : nodeType;
        }

        public Cluster GetClusterById(long clusterId)
        {
            Cluster cluster = _unitOfWork.ClusterRepository.GetById(clusterId);
            return cluster == null ? throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId) : cluster;
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
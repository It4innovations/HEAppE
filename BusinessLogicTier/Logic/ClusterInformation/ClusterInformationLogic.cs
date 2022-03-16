using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using log4net;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation
{
    internal class ClusterInformationLogic : IClusterInformationLogic {
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly IUnitOfWork unitOfWork;

        internal ClusterInformationLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IList<Cluster> ListAvailableClusters()
        {
            return unitOfWork.ClusterRepository.GetAll().ToList();
        }

        public ClusterNodeUsage GetCurrentClusterNodeUsage(long clusterNodeId, AdaptorUser loggedUser)
        {
            ClusterNodeType nodeType = GetClusterNodeTypeById(clusterNodeId);
            return SchedulerFactory.GetInstance(nodeType.Cluster.SchedulerType).CreateScheduler(nodeType.Cluster)
                                    .GetCurrentClusterNodeUsage(nodeType);

        }

        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, AdaptorUser loggedUser)
        {
            CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
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
                var commandTemplateParameters = new List<string>() { scriptPath };
                commandTemplateParameters.AddRange(SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).GetParametersFromGenericUserScript(cluster, userScriptPath).ToList());
                return commandTemplateParameters;
            }
            else
            {
                return commandTemplate.TemplateParameters.Select(s => s.Identifier)
                                                          .ToList();
            }
        }

        public ClusterAuthenticationCredentials GetNextAvailableUserCredentials(long clusterId)
        {
            // Get credentials for cluster
            Cluster cluster = unitOfWork.ClusterRepository.GetById(clusterId);
            if (cluster == null)
            {
                log.Error("Requested cluster with Id=" + clusterId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster with Id=" + clusterId + " does not exist in the system.");
            }
            List<ClusterAuthenticationCredentials> credentials =
                (from account in cluster.AuthenticationCredentials where account != cluster.ServiceAccountCredentials orderby account.Id ascending select account).ToList();

            long lastUsedId = ClusterUserCache.GetLastUserId(cluster);
            if (lastUsedId == 0)
            {   // No user has been used from this cluster
                // return first usable account
                ClusterUserCache.SetLastUserId(cluster, credentials[0].Id);
                log.DebugFormat("Using initial cluster account: {0}", credentials[0].Username);
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
                ClusterUserCache.SetLastUserId(cluster, creds.Id);
                log.DebugFormat("Using cluster account: {0}", creds.Username);
                return creds;
            }

        }

        public ClusterNodeType GetClusterNodeTypeById(long clusterNodeTypeId)
        {
            ClusterNodeType nodeType = unitOfWork.ClusterNodeTypeRepository.GetById(clusterNodeTypeId);
            if (nodeType == null)
            {
                log.Error("Requested cluster node type with Id=" + clusterNodeTypeId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster node type with Id=" + clusterNodeTypeId + " does not exist in the system.");
            }
            return nodeType;
        }

        public Cluster GetClusterById(long clusterId)
        {
            Cluster cluster = unitOfWork.ClusterRepository.GetById(clusterId);
            if (cluster == null)
            {
                log.Error("Requested cluster with Id=" + clusterId + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested cluster with Id=" + clusterId + " does not exist in the system.");
            }
            return cluster;
        }

        public IList<ClusterNodeType> ListClusterNodeTypes()
        {
            return unitOfWork.ClusterNodeTypeRepository.GetAll();
        }

        public bool IsUserAvailableToRun(ClusterAuthenticationCredentials user)
        {
            //List all unfinished jobs and 
            IEnumerable<SubmittedJobInfo> allRunningJobs = unitOfWork.SubmittedJobInfoRepository.GetAllUnfinished();
            List<SubmittedJobInfo> userRunningJobs = allRunningJobs.Where(w => w.Specification.ClusterUser == user && (w.State == JobState.Running
                                                                                                                            || w.State == JobState.Queued
                                                                                                                                || w.State == JobState.Submitted))
                                                                    .ToList();

            return userRunningJobs.Any() ? false : true;
        }

    }
}
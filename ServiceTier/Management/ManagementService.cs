using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ServiceTier.Management
{
    public class ManagementService : IManagementService
    {
        #region Instances

        private readonly ILog _logger;

        #endregion
        #region Constructors

        public ManagementService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        #endregion
        #region IManagementService Methods

        public ExtendedCommandTemplateExt CreateCommandTemplateModel(string modelName, string modelDescription,
            string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
            long modelProjectId, long modelClusterNodeTypeId, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, modelProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate commandTemplate = managementLogic.CreateCommandTemplate(modelName, modelDescription, modelExtendedAllocationCommand, modelExecutableFile, modelPreparationScript, modelProjectId, modelClusterNodeTypeId);
                return commandTemplate.ConvertIntToExtendedExt();
            }
        }

        public CommandTemplateExt CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name, long projectId,
            string description, string extendedAllocationCommand, string executableFile, string preparationScript, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Maintainer, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate commandTemplate = managementLogic.CreateCommandTemplateFromGeneric(genericCommandTemplateId, name, projectId, description, extendedAllocationCommand, executableFile, preparationScript);
                return commandTemplate.ConvertIntToExt();
            }

        }

        public ExtendedCommandTemplateExt ModifyCommandTemplateModel(long modelId, string modelName, string modelDescription,
            string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
            long modelClusterNodeTypeId, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(modelId)
                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                if (!commandTemplate.ProjectId.HasValue || !commandTemplate.IsEnabled)
                {
                    throw new InputValidationException("NotPermitted");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate updatedCommandTemplate = managementLogic.ModifyCommandTemplate(modelId, modelName, modelDescription, modelExtendedAllocationCommand, modelExecutableFile, modelPreparationScript, modelClusterNodeTypeId);
                return updatedCommandTemplate.ConvertIntToExtendedExt();
            }
        }

        public CommandTemplateExt ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId,
            string description, string extendedAllocationCommand, string executableFile, string preparationScript, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Maintainer, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate commandTemplate = managementLogic.ModifyCommandTemplateFromGeneric(commandTemplateId, name, projectId, description, extendedAllocationCommand, executableFile, preparationScript);
                return commandTemplate.ConvertIntToExt();
            }
        }

        public void RemoveCommandTemplate(long commandTemplateId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

                if (commandTemplate.ProjectId == null)
                {
                    throw new InputValidationException("The specified command template cannot be removed!");
                }

                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Maintainer, commandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveCommandTemplate(commandTemplateId);
            }
        }

        public ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Project project = managementLogic.CreateProject(accountingString, usageType, name, description, startDate, endDate, useAccountingStringForScheduler, piEmail, loggedUser);
                return project.ConvertIntToExt();
            }
        }

        public ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, id);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Project project = managementLogic.ModifyProject(id, usageType, name, description, startDate, endDate, useAccountingStringForScheduler);
                return project.ConvertIntToExt();
            }
        }

        public void RemoveProject(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, id);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveProject(id);
            }
        }

        public ClusterProjectExt GetProjectAssignmentToClusterById(long projectId, long clusterId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProject clusterProject = managementLogic.GetProjectAssignmentToClusterById(projectId, clusterId);
                return clusterProject.ConvertIntToExt();
            }
        }

        public ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProject clusterProject = managementLogic.CreateProjectAssignmentToCluster(projectId, clusterId, localBasepath);
                return clusterProject.ConvertIntToExt();
            }
        }

        public ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProject clusterProject = managementLogic.ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);
                return clusterProject.ConvertIntToExt();
            }
        }

        public void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveProjectAssignmentToCluster(projectId, clusterId);
            }
        }

        public List<PublicKeyExt> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.CreateSecureShellKey(credentials, projectId).Select(x => x.ConvertIntToExt()).ToList();
            }
        }

        public PublicKeyExt RegenerateSecureShellKey(string username, string password, string publicKey, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return string.IsNullOrEmpty(username)
                    ? managementLogic.RegenerateSecureShellKeyByPublicKey(publicKey, password, projectId).ConvertIntToExt()
                    : managementLogic.RegenerateSecureShellKey(username, password, projectId).ConvertIntToExt();
            }
        }

        public void RemoveSecureShellKey(string username, string publicKey, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);

                if (string.IsNullOrEmpty(username))
                {
                    managementLogic.RemoveSecureShellKeyByPublicKey(publicKey, projectId);
                }
                else
                {
                    managementLogic.RemoveSecureShellKey(username, projectId);
                }
            }
        }

        public List<ClusterInitReportExt> InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.InitializeClusterScriptDirectory(projectId, clusterProjectRootDirectory).Select(x => x.ConvertIntToExt()).ToList();
            }
        }

        public bool TestClusterAccessForAccount(long modelProjectId, string modelSessionCode, string username)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, modelProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.TestClusterAccessForAccount(modelProjectId, username);
            }
        }

        public ExtendedCommandTemplateParameterExt GetCommandTemplateParameterById(long id, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplateParameter commandTemplateParameter = managementLogic.GetCommandTemplateParameterById(id);
                return commandTemplateParameter.ConvertIntToExtendedExt();
            }
        }

        public ExtendedCommandTemplateParameterExt CreateCommandTemplateParameter(string modelIdentifier,
            string modelQuery,
            string modelDescription, long modelCommandTemplateId, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(modelCommandTemplateId)
                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplateParameter commandTemplateParameter = managementLogic.CreateCommandTemplateParameter(modelIdentifier, modelQuery, modelDescription, modelCommandTemplateId);
                return commandTemplateParameter.ConvertIntToExtendedExt();
            }
        }

        public ExtendedCommandTemplateParameterExt ModifyCommandTemplateParameter(long id, string modelIdentifier, string modelQuery,
            string modelDescription, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplateParameter commandTemplateParameter = unitOfWork.CommandTemplateParameterRepository.GetById(id)
                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound", id);
                if (!commandTemplateParameter.IsEnabled)
                {
                    //unauthorized
                    throw new InputValidationException("NotPermitted");
                }

                //command template not found or not enabled
                if (!commandTemplateParameter.CommandTemplate.ProjectId.HasValue)
                {
                    throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                }

                if (!commandTemplateParameter.CommandTemplate.IsEnabled)
                {
                    throw new InputValidationException("NotPermitted");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, commandTemplateParameter.CommandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplateParameter updatedCommandTemplateParameter = managementLogic.ModifyCommandTemplateParameter(id, modelIdentifier, modelQuery, modelDescription);
                return updatedCommandTemplateParameter.ConvertIntToExtendedExt();
            }
        }

        public string RemoveCommandTemplateParameter(long id, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplateParameter commandTemplateParameter = unitOfWork.CommandTemplateParameterRepository.GetById(id)
                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound", id);
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, commandTemplateParameter.CommandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveCommandTemplateParameter(id);
                return $"CommandTemplateParameter id {id} was removed";
            }
        }

        public List<ExtendedCommandTemplateExt> ListCommandTemplates(long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.ListCommandTemplates(projectId).Select(x => x.ConvertIntToExtendedExt()).ToList();
            }
        }

        public ExtendedCommandTemplateExt ListCommandTemplate(long commandTemplateId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                                    ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                if (!commandTemplate.IsEnabled)
                {
                    throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                }
                if (!commandTemplate.ProjectId.HasValue)
                {
                    (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager);
                    return commandTemplate.ConvertIntToExtendedExt();
                }
                else
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value);
                    return commandTemplate.ConvertIntToExtendedExt();
                }
            }
        }

        public SubProjectExt ListSubProject(long subProjectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                SubProject subProject = unitOfWork.SubProjectRepository.GetById(subProjectId)
                    ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager, subProject.ProjectId);
                return subProject.ConvertIntToExt();
            }
        }
        
        public List<SubProjectExt> ListSubProjects(long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                Project project = unitOfWork.ProjectRepository.GetById(projectId)
                                        ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Manager, projectId);
                List<SubProject> subProjects = project.SubProjects.ToList();
                return subProjects.Select(x => x.ConvertIntToExt()).ToList();
            }
        }

        public SubProjectExt CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription,
            DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, modelProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                SubProject subProject = managementLogic.CreateSubProject(modelProjectId, modelIdentifier, modelDescription, modelStartDate, modelEndDate);
                return subProject.ConvertIntToExt();
            }
        }

        public SubProjectExt ModifySubProject(long modelId, string modelIdentifier, string modelDescription, DateTime modelStartDate,
            DateTime? modelEndDate, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                SubProject subProject = unitOfWork.SubProjectRepository.GetById(modelId)
                    ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, subProject.ProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                
                SubProject updatedSubProject = managementLogic.ModifySubProject(modelId, modelIdentifier, modelDescription, modelStartDate, modelEndDate);
                return updatedSubProject.ConvertIntToExt();
            }
        }

        public void RemoveSubProject(long modelId, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                SubProject subProject = unitOfWork.SubProjectRepository.GetById(modelId)
                    ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Manager, subProject.ProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveSubProject(modelId);
            }
        }

        public void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (var user, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, AdaptorUserRoleType.Administrator);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                foreach (var project in projects)
                {
                    managementLogic.ComputeAccounting(modelStartTime, modelEndTime, project.Id);
                }
            }
        }

        public ClusterExt GetClusterById(long clusterId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Cluster cluster = managementLogic.GetClusterById(clusterId);
                return cluster.ConvertIntToExt();
            }
        }

        public ClusterExt CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Cluster cluster = managementLogic.CreateCluster(name, description, masterNodeName, schedulerType, clusterConnectionProtocol,
                    timeZone, port, updateJobStateByServiceAccount, domainName, proxyConnectionId);
                return cluster.ConvertIntToExt();
            }
        }

        public ClusterExt ModifyCluster(long id, string name, string description, string masterNodeName, SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
            string timeZone, int port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Cluster cluster = managementLogic.ModifyCluster(id, name, description, masterNodeName, schedulerType, clusterConnectionProtocol,
                    timeZone, port, updateJobStateByServiceAccount, domainName, proxyConnectionId);
                return cluster.ConvertIntToExt();
            }
        }

        public void RemoveCluster(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveCluster(id);
            }
        }

        public ClusterNodeTypeExt GetClusterNodeTypeById(long clusterId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeType clusterNodeType = managementLogic.GetClusterNodeTypeById(clusterId);
                return clusterNodeType.ConvertIntToExt();
            }
        }

        public ClusterNodeTypeExt CreateClusterNodeType(string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService, int? maxWalltime,
            string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeType clusterNodeType = managementLogic.CreateClusterNodeType(name, description, numberOfNodes, coresPerNode, queue, qualityOfService, maxWalltime,
                    clusterAllocationName, clusterId, fileTransferMethodId, clusterNodeTypeAggregationId);
                return clusterNodeType.ConvertIntToExt();
            }
        }

        public ClusterNodeTypeExt ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes, int coresPerNode, string queue, string qualityOfService,
            int? maxWalltime, string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeType clusterNodeType = managementLogic.ModifyClusterNodeType(id, name, description, numberOfNodes, coresPerNode, queue, qualityOfService, maxWalltime,
                    clusterAllocationName, clusterId, fileTransferMethodId, clusterNodeTypeAggregationId);
                return clusterNodeType.ConvertIntToExt();
            }
        }

        public void RemoveClusterNodeType(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveClusterNodeType(id);
            }
        }

        public ClusterProxyConnectionExt GetClusterProxyConnectionById(long clusterProxyConnectionId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProxyConnection clusterProxyConnection = managementLogic.GetClusterProxyConnectionById(clusterProxyConnectionId);
                return clusterProxyConnection.ConvertIntToExt();
            } 
        }

        public ClusterProxyConnectionExt CreateClusterProxyConnection(string host, int port, string username, string password, ProxyType type, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProxyConnection clusterProxyConnection = managementLogic.CreateClusterProxyConnection(host, port, username, password, type);
                return clusterProxyConnection.ConvertIntToExt();
            }
        }

        public ClusterProxyConnectionExt ModifyClusterProxyConnection(long id, string host, int port, string username, string password, ProxyType type, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProxyConnection clusterProxyConnection = managementLogic.ModifyClusterProxyConnection(id, host, port, username, password, type);
                return clusterProxyConnection.ConvertIntToExt();
            }
        }

        public void RemoveClusterProxyConnection(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveClusterProxyConnection(id);
            }
        }

        public FileTransferMethodExt GetFileTransferMethodById(long fileTransferMethodId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                FileTransferMethod fileTransferMethod = managementLogic.GetFileTransferMethodById(fileTransferMethodId);
                return fileTransferMethod.ConvertIntToExt();
            }
        }

        public FileTransferMethodExt CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol, long clusterId, int? port, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                FileTransferMethod fileTransferMethod = managementLogic.CreateFileTransferMethod(serverHostname, protocol, clusterId, port);
                return fileTransferMethod.ConvertIntToExt();
            }
        }

        public FileTransferMethodExt ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol, long clusterId, int? port, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                FileTransferMethod fileTransferMethod = managementLogic.ModifyFileTransferMethod(id, serverHostname, protocol, clusterId, port);
                return fileTransferMethod.ConvertIntToExt();
            }
        }

        public void RemoveFileTransferMethod(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveFileTransferMethod(id);
            }
        }

        public ClusterNodeTypeAggregationExt GetClusterNodeTypeAggregationById(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeTypeAggregation clusterNodeTypeAggregation = managementLogic.GetClusterNodeTypeAggregationById(id);
                return clusterNodeTypeAggregation.ConvertIntToExt();
            }
        }

        public List<ClusterNodeTypeAggregationExt> GetClusterNodeTypeAggregations(string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                List<ClusterNodeTypeAggregation> clusterNodeTypeAggregation = managementLogic.GetClusterNodeTypeAggregations();
                return clusterNodeTypeAggregation.Select(cna => cna.ConvertIntToExt()).ToList();
            }
        }

        public ClusterNodeTypeAggregationExt CreateClusterNodeTypeAggregation(string name, string description, string allocationType, DateTime validityFrom,
            DateTime? validityTo, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeTypeAggregation clusterNodeTypeAggregation = managementLogic.CreateClusterNodeTypeAggregation(name, description, allocationType, validityFrom,
                    validityTo);
                return clusterNodeTypeAggregation.ConvertIntToExt();
            }
        }

        public ClusterNodeTypeAggregationExt ModifyClusterNodeTypeAggregation(long id, string name, string description, string allocationType, DateTime validityFrom,
            DateTime? validityTo, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeTypeAggregation clusterNodeTypeAggregation = managementLogic.ModifyClusterNodeTypeAggregation(id, name, description, allocationType, validityFrom,
                    validityTo);
                return clusterNodeTypeAggregation.ConvertIntToExt();
            }
        }

        public void RemoveClusterNodeTypeAggregation(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveClusterNodeTypeAggregation(id);
            }
        }

        public ClusterNodeTypeAggregationAccountingExt GetClusterNodeTypeAggregationAccountingById(long clusterNodeTypeAggregationId, long accountingId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeTypeAggregationAccounting clusterNodeTypeAggregationAccounting = managementLogic.GetClusterNodeTypeAggregationAccountingById(clusterNodeTypeAggregationId, accountingId);
                return clusterNodeTypeAggregationAccounting.ConvertIntToExt();
            }
        }

        public ClusterNodeTypeAggregationAccountingExt CreateClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterNodeTypeAggregationAccounting clusterNodeTypeAggregationAccounting = managementLogic.CreateClusterNodeTypeAggregationAccounting(clusterNodeTypeAggregationId, accountingId);
                return clusterNodeTypeAggregationAccounting.ConvertIntToExt();
            }
        }

        public void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveClusterNodeTypeAggregationAccounting(clusterNodeTypeAggregationId, accountingId);
            }
        }

        public AccountingExt GetAccountingById(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Accounting accounting = managementLogic.GetAccountingById(id);
                return accounting.ConvertIntToExt();
            }
        }

        public AccountingExt CreateAccounting(string formula, DateTime validityFrom, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Accounting accounting = managementLogic.CreateAccounting(formula, validityFrom);
                return accounting.ConvertIntToExt();
            }
        }

        public AccountingExt ModifyAccounting(long id, string formula, DateTime validityFrom, DateTime? validityTo, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Accounting accounting = managementLogic.ModifyAccounting(id, formula, validityFrom, validityTo);
                return accounting.ConvertIntToExt();
            }
        }

        public void RemoveAccounting(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveAccounting(id);
            }
        }

        public ProjectClusterNodeTypeAggregationExt GetProjectClusterNodeTypeAggregationById(long projectId, long clusterNodeTypeAggregationId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ProjectClusterNodeTypeAggregation aggregation = managementLogic.GetProjectClusterNodeTypeAggregationById(projectId, clusterNodeTypeAggregationId);
                return aggregation.ConvertIntToExt();
            }
        }

        public List<ProjectClusterNodeTypeAggregationExt> GetProjectClusterNodeTypeAggregationsByProjectId(long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                List<ProjectClusterNodeTypeAggregation> aggregations = managementLogic.GetProjectClusterNodeTypeAggregationsByProjectId(projectId);
                return aggregations.Select(pcna => pcna.ConvertIntToExt()).ToList();
            }
        }

        public ProjectClusterNodeTypeAggregationExt CreateProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId, long allocationAmount, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ProjectClusterNodeTypeAggregation aggregation = managementLogic.CreateProjectClusterNodeTypeAggregation(projectId, clusterNodeTypeAggregationId, allocationAmount);
                return aggregation.ConvertIntToExt();
            }
        }

        public void RemoveProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                (AdaptorUser loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveProjectClusterNodeTypeAggregation(projectId, clusterNodeTypeAggregationId);
            }
        }
        #endregion
    }
}
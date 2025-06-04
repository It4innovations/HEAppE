using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobReporting.Enums;
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

namespace HEAppE.ServiceTier.Management;

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
        _logger.Info(
            $"CreateCommandTemplateModel: Name: {modelName}, Description: {modelDescription}, ExtendedAllocationCommand: {modelExtendedAllocationCommand}, ExecutableFile: {modelExecutableFile}, PreparationScript: {modelPreparationScript}, ProjectId: {modelProjectId}, ClusterNodeTypeId: {modelClusterNodeTypeId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, modelProjectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var commandTemplate = managementLogic.CreateCommandTemplate(modelName, modelDescription,
                modelExtendedAllocationCommand, modelExecutableFile, modelPreparationScript, modelProjectId,
                modelClusterNodeTypeId);
            return commandTemplate.ConvertIntToExtendedExt();
        }
    }

    public CommandTemplateExt CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name,
        long projectId,
        string description, string extendedAllocationCommand, string executableFile, string preparationScript,
        string sessionCode)
    {
        _logger.Info(
            $"CreateCommandTemplateFromGeneric: GenericCommandTemplateId: {genericCommandTemplateId}, Name: {name}, ProjectId: {projectId}, Description: {description}, ExtendedAllocationCommand: {extendedAllocationCommand}, ExecutableFile: {executableFile}, PreparationScript: {preparationScript}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Maintainer, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var commandTemplate = managementLogic.CreateCommandTemplateFromGeneric(genericCommandTemplateId, name,
                projectId, description, extendedAllocationCommand, executableFile, preparationScript,
                adaptorUserId: loggedUser.Id);
            return commandTemplate.ConvertIntToExt();
        }
    }

    public ExtendedCommandTemplateExt ModifyCommandTemplateModel(long modelId, string modelName,
        string modelDescription,
        string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
        long modelClusterNodeTypeId, bool modelIsEnabled, string modelSessionCode)
    {
        _logger.Info(
            $"ModifyCommandTemplateModel: Id: {modelId}, Name: {modelName}, Description: {modelDescription}, ExtendedAllocationCommand: {modelExtendedAllocationCommand}, ExecutableFile: {modelExecutableFile}, PreparationScript: {modelPreparationScript}, ClusterNodeTypeId: {modelClusterNodeTypeId}, IsEnabled: {modelIsEnabled}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplate = unitOfWork.CommandTemplateRepository.GetById(modelId)
                                  ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            if (!commandTemplate.ProjectId.HasValue || commandTemplate.IsDeleted)
                throw new InputValidationException("NotPermitted");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var updatedCommandTemplate = managementLogic.ModifyCommandTemplate(modelId, modelName, modelDescription,
                modelExtendedAllocationCommand, modelExecutableFile, modelPreparationScript, modelClusterNodeTypeId,
                modelIsEnabled);
            return updatedCommandTemplate.ConvertIntToExtendedExt();
        }
    }

    public CommandTemplateExt ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId,
        string description, string extendedAllocationCommand, string executableFile, string preparationScript,
        string sessionCode)
    {
        _logger.Info(
            $"ModifyCommandTemplateFromGeneric: Id: {commandTemplateId}, Name: {name}, ProjectId: {projectId}, Description: {description}, ExtendedAllocationCommand: {extendedAllocationCommand}, ExecutableFile: {executableFile}, PreparationScript: {preparationScript}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Maintainer, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId) ??
                                  throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            if (!commandTemplate.ProjectId.HasValue || commandTemplate.IsDeleted)
                throw new InputValidationException("NotPermitted");
            var updatedCommandTemplate = managementLogic.ModifyCommandTemplateFromGeneric(commandTemplateId, name, projectId,
                description, extendedAllocationCommand, executableFile, preparationScript,
                adaptorUserId: loggedUser.Id);
            return updatedCommandTemplate.ConvertIntToExt();
        }
    }

    public void RemoveCommandTemplate(long commandTemplateId, string sessionCode)
    {
        _logger.Info($"RemoveCommandTemplate: Id: {commandTemplateId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                                  ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

            if (commandTemplate.ProjectId == null)
                throw new InputValidationException("The specified command template cannot be removed!");

            UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Maintainer, commandTemplate.ProjectId.Value, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveCommandTemplate(commandTemplateId);
        }
    }

    public ProjectExt GetProjectByAccountingString(string accountingString, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var project = managementLogic.GetProjectByAccountingString(accountingString);
            return project.ConvertIntToExt();
        }
    }

    public ProjectExt GetProjectById(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var project = managementLogic.GetProjectById(id);
            return project.ConvertIntToExt();
        }
    }

    public ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description,
        DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, bool isOneToOneMapping, string sessionCode)
    {
        _logger.Info(
            $"CreateProject: AccountingString: {accountingString}, UsageType: {usageType}, Name: {name}, Description: {description}, StartDate: {startDate}, EndDate: {endDate}, UseAccountingStringForScheduler: {useAccountingStringForScheduler}, PiEmail: {piEmail}, IsOneToOneMapping: {isOneToOneMapping}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var project = managementLogic.CreateProject(accountingString, usageType, name, description, startDate,
                endDate, useAccountingStringForScheduler, piEmail, isOneToOneMapping, loggedUser);
            return project.ConvertIntToExt();
        }
    }

    public ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, DateTime startDate,
        DateTime endDate, bool? useAccountingStringForScheduler, bool isOneToOneMapping, string sessionCode)
    {
        _logger.Info(
            $"ModifyProject: Id: {id}, UsageType: {usageType}, Name: {name}, Description: {description}, StartDate: {startDate}, EndDate: {endDate}, UseAccountingStringForScheduler: {useAccountingStringForScheduler}, IsOneToOneMapping: {isOneToOneMapping}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, id, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var project = managementLogic.ModifyProject(id, usageType, name, description, startDate, endDate,
                useAccountingStringForScheduler, isOneToOneMapping);
            return project.ConvertIntToExt();
        }
    }

    public void RemoveProject(long id, string sessionCode)
    {
        _logger.Info($"RemoveProject: Id: {id}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, id, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveProject(id);
        }
    }

    public ClusterProjectExt GetProjectAssignmentToClusterById(long projectId, long clusterId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProject = managementLogic.GetProjectAssignmentToClusterById(projectId, clusterId);
            return clusterProject.ConvertIntToExt();
        }
    }

    public ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath,
        string sessionCode)
    {
        _logger.Info(
            $"CreateProjectAssignmentToCluster: ProjectId: {projectId}, ClusterId: {clusterId}, LocalBasepath: {localBasepath}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProject = managementLogic.CreateProjectAssignmentToCluster(projectId, clusterId, localBasepath);
            return clusterProject.ConvertIntToExt();
        }
    }

    public ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath,
        string sessionCode)
    {
        _logger.Info(
            $"ModifyProjectAssignmentToCluster: ProjectId: {projectId}, ClusterId: {clusterId}, LocalBasepath: {localBasepath}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProject = managementLogic.ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);
            return clusterProject.ConvertIntToExt();
        }
    }

    public void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode)
    {
        _logger.Info($"RemoveProjectAssignmentToCluster: ProjectId: {projectId}, ClusterId: {clusterId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveProjectAssignmentToCluster(projectId, clusterId);
        }
    }

    public List<PublicKeyExt> GetSecureShellKeys(long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.GetSecureShellKeys(projectId,
                adaptorUserId: loggedUser.Id).Select(x => x.ConvertIntToExt()).ToList();
        }
    }


    public List<PublicKeyExt> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.CreateSecureShellKey(credentials, projectId,
                adaptorUserId: loggedUser.Id).Select(x => x.ConvertIntToExt()).ToList();
        }
    }

    public PublicKeyExt RegenerateSecureShellKey(string username, string password, string publicKey, long projectId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return string.IsNullOrEmpty(username)
                ? managementLogic.RegenerateSecureShellKeyByPublicKey(publicKey, password, projectId).ConvertIntToExt()
                : managementLogic.RegenerateSecureShellKey(username, password, projectId).ConvertIntToExt();
        }
    }

    public void RemoveSecureShellKey(string username, string publicKey, long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);

            if (string.IsNullOrEmpty(username))
                managementLogic.RemoveSecureShellKeyByPublicKey(publicKey, projectId);
            else
                managementLogic.RemoveSecureShellKey(username, projectId);
        }
    }

    public List<ClusterInitReportExt> InitializeClusterScriptDirectory(long projectId,
        string clusterProjectRootDirectory, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.ManagementAdmin, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.InitializeClusterScriptDirectory(projectId, clusterProjectRootDirectory,
                adaptorUserId: loggedUser.Id).Select(x => x.ConvertIntToExt()).ToList();
        }
    }

    public bool TestClusterAccessForAccount(long modelProjectId, string modelSessionCode, string username)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.ManagementAdmin, modelProjectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.TestClusterAccessForAccount(modelProjectId, username);
        }
    }

    public ExtendedCommandTemplateParameterExt GetCommandTemplateParameterById(long id, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var commandTemplateParameter = managementLogic.GetCommandTemplateParameterById(id);
            return commandTemplateParameter.ConvertIntToExtendedExt();
        }
    }

    public ExtendedCommandTemplateParameterExt CreateCommandTemplateParameter(string modelIdentifier,
        string modelQuery,
        string modelDescription, long modelCommandTemplateId, string modelSessionCode)
    {
        _logger.Info(
            $"CreateCommandTemplateParameter: Identifier: {modelIdentifier}, Query: {modelQuery}, Description: {modelDescription}, CommandTemplateId: {modelCommandTemplateId}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplate = unitOfWork.CommandTemplateRepository.GetById(modelCommandTemplateId)
                                  ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var commandTemplateParameter = managementLogic.CreateCommandTemplateParameter(modelIdentifier, modelQuery,
                modelDescription, modelCommandTemplateId);
            return commandTemplateParameter.ConvertIntToExtendedExt();
        }
    }

    public ExtendedCommandTemplateParameterExt ModifyCommandTemplateParameter(long id, string modelIdentifier,
        string modelQuery,
        string modelDescription, string modelSessionCode)
    {
        _logger.Info(
            $"ModifyCommandTemplateParameter: Id: {id}, Identifier: {modelIdentifier}, Query: {modelQuery}, Description: {modelDescription}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplateParameter = unitOfWork.CommandTemplateParameterRepository.GetById(id)
                                           ?? throw new RequestedObjectDoesNotExistException(
                                               "CommandTemplateParameterNotFound", id);

            //command template not found or not enabled
            if (!commandTemplateParameter.CommandTemplate.ProjectId.HasValue)
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

            if (commandTemplateParameter.CommandTemplate.IsDeleted) throw new InputValidationException("NotPermitted");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, commandTemplateParameter.CommandTemplate.ProjectId.Value);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var updatedCommandTemplateParameter =
                managementLogic.ModifyCommandTemplateParameter(id, modelIdentifier, modelQuery, modelDescription);
            return updatedCommandTemplateParameter.ConvertIntToExtendedExt();
        }
    }

    public string RemoveCommandTemplateParameter(long id, string modelSessionCode)
    {
        _logger.Info($"RemoveCommandTemplateParameter: Id: {id}");
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplateParameter = unitOfWork.CommandTemplateParameterRepository.GetById(id)
                                           ?? throw new RequestedObjectDoesNotExistException(
                                               "CommandTemplateParameterNotFound", id);

            if (!commandTemplateParameter.CommandTemplate.ProjectId.HasValue)
                throw new InputValidationException("The specified command template parameter cannot be removed!");

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, commandTemplateParameter.CommandTemplate.ProjectId.Value,
                true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveCommandTemplateParameter(id);
            return $"CommandTemplateParameter id {id} was removed";
        }
    }

    public List<ExtendedCommandTemplateExt> ListCommandTemplates(long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Manager, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.ListCommandTemplates(projectId).Select(x => x.ConvertIntToExtendedExt()).ToList();
        }
    }

    public ExtendedCommandTemplateExt ListCommandTemplate(long commandTemplateId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId)
                                  ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            if (commandTemplate.IsDeleted) throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            if (!commandTemplate.ProjectId.HasValue)
            {
                (var loggedUser, _) =
                    UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                        AdaptorUserRoleType.Manager);
                return commandTemplate.ConvertIntToExtendedExt();
            }
            else
            {
                var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode,
                    unitOfWork, AdaptorUserRoleType.Manager, commandTemplate.ProjectId.Value, true);
                return commandTemplate.ConvertIntToExtendedExt();
            }
        }
    }

    public SubProjectExt ListSubProject(long subProjectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var subProject = unitOfWork.SubProjectRepository.GetById(subProjectId)
                             ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Manager, subProject.ProjectId, true);
            return subProject.ConvertIntToExt();
        }
    }

    public List<SubProjectExt> ListSubProjects(long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var project = unitOfWork.ProjectRepository.GetById(projectId)
                          ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Manager, projectId, true);
            var subProjects = project.SubProjects.ToList();
            return subProjects.Select(x => x.ConvertIntToExt()).ToList();
        }
    }

    public SubProjectExt CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate, DateTime? modelEndDate, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, modelProjectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var subProject = managementLogic.CreateSubProject(modelProjectId, modelIdentifier, modelDescription,
                modelStartDate, modelEndDate);
            return subProject.ConvertIntToExt();
        }
    }

    public SubProjectExt ModifySubProject(long modelId, string modelIdentifier, string modelDescription,
        DateTime modelStartDate,
        DateTime? modelEndDate, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var subProject = unitOfWork.SubProjectRepository.GetById(modelId)
                             ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, subProject.ProjectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);

            var updatedSubProject = managementLogic.ModifySubProject(modelId, modelIdentifier, modelDescription,
                modelStartDate, modelEndDate);
            return updatedSubProject.ConvertIntToExt();
        }
    }

    public void RemoveSubProject(long modelId, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var subProject = unitOfWork.SubProjectRepository.GetById(modelId)
                             ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode,
                unitOfWork, AdaptorUserRoleType.Manager, subProject.ProjectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveSubProject(modelId);
        }
    }

    public void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, long projectId,
        string modelSessionCode)
    {
        Task.Run(() => ComputeAccountingTask(modelStartTime, modelEndTime, projectId, modelSessionCode))
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.Error(
                        $"Error while computing accounting for project {projectId} with session code {modelSessionCode}: {t.Exception}");
            });
    }

    private void ComputeAccountingTask(DateTime modelStartTime, DateTime modelEndTime, long projectId,
        string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var user = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork,
                AdaptorUserRoleType.Manager, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            _logger.Info(
                $"User {user.Username} is computing accounting for project {projectId} from {modelStartTime} to {modelEndTime}");
            managementLogic.ComputeAccounting(modelStartTime, modelEndTime, projectId);
        }
    }

    public List<AccountingStateExt> ListAccountingStates(long projectId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var user = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Manager, projectId, true);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            return managementLogic.ListAccountingStates(projectId).Select(x => x.ConvertIntToExt()).ToList();
        }
    }

    public ExtendedClusterExt GetClusterById(long clusterId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var cluster = managementLogic.GetByIdWithProxyConnection(clusterId);
            return cluster.ConvertIntToExtendedExt(projects, false);
        }
    }
    
    public List<ExtendedClusterExt> GetClusters(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
            var clusters = clusterLogic.ListAvailableClusters().Select(s => s.ConvertIntToExtendedExt(projects, false)).ToList();
            return clusters;
        }
    }

    public ExtendedClusterExt CreateCluster(string name, string description, string masterNodeName, SchedulerType schedulerType,
        ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var cluster = managementLogic.CreateCluster(name, description, masterNodeName, schedulerType,
                clusterConnectionProtocol,
                timeZone, port, updateJobStateByServiceAccount, domainName, proxyConnectionId);
            return cluster.ConvertIntToExtendedExt(projects, false);
        }
    }

    public ExtendedClusterExt ModifyCluster(long id, string name, string description, string masterNodeName,
        SchedulerType schedulerType, ClusterConnectionProtocol clusterConnectionProtocol,
        string timeZone, int? port, bool updateJobStateByServiceAccount, string domainName, long? proxyConnectionId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var cluster = managementLogic.ModifyCluster(id, name, description, masterNodeName, schedulerType,
                clusterConnectionProtocol,
                timeZone, port, updateJobStateByServiceAccount, domainName, proxyConnectionId);
            return cluster.ConvertIntToExtendedExt(projects, false);
        }
    }

    public void RemoveCluster(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveCluster(id);
        }
    }

    public ClusterNodeTypeExt GetClusterNodeTypeById(long clusterId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeType = managementLogic.GetClusterNodeTypeById(clusterId);
            return clusterNodeType.ConvertIntToExt(projects, false);
        }
    }

    public ClusterNodeTypeExt CreateClusterNodeType(string name, string description, int? numberOfNodes,
        int coresPerNode, string queue, string qualityOfService, int? maxWalltime,
        string clusterAllocationName, long? clusterId, long? fileTransferMethodId, long? clusterNodeTypeAggregationId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeType = managementLogic.CreateClusterNodeType(name, description, numberOfNodes, coresPerNode,
                queue, qualityOfService, maxWalltime,
                clusterAllocationName, clusterId, fileTransferMethodId, clusterNodeTypeAggregationId);
            return clusterNodeType.ConvertIntToExt(projects, false);
        }
    }

    public ClusterNodeTypeExt ModifyClusterNodeType(long id, string name, string description, int? numberOfNodes,
        int coresPerNode, string queue, string qualityOfService,
        int? maxWalltime, string clusterAllocationName, long? clusterId, long? fileTransferMethodId,
        long? clusterNodeTypeAggregationId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeType = managementLogic.ModifyClusterNodeType(id, name, description, numberOfNodes,
                coresPerNode, queue, qualityOfService, maxWalltime,
                clusterAllocationName, clusterId, fileTransferMethodId, clusterNodeTypeAggregationId);
            return clusterNodeType.ConvertIntToExt(projects, false);
        }
    }

    public void RemoveClusterNodeType(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveClusterNodeType(id);
        }
    }

    public ClusterProxyConnectionExt GetClusterProxyConnectionById(long clusterProxyConnectionId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProxyConnection = managementLogic.GetClusterProxyConnectionById(clusterProxyConnectionId);
            return clusterProxyConnection.ConvertIntToExt();
        }
    }

    public ClusterProxyConnectionExt CreateClusterProxyConnection(string host, int port, string username,
        string password, ProxyType type, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProxyConnection =
                managementLogic.CreateClusterProxyConnection(host, port, username, password, type);
            return clusterProxyConnection.ConvertIntToExt();
        }
    }

    public ClusterProxyConnectionExt ModifyClusterProxyConnection(long id, string host, int port, string username,
        string password, ProxyType type, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterProxyConnection =
                managementLogic.ModifyClusterProxyConnection(id, host, port, username, password, type);
            return clusterProxyConnection.ConvertIntToExt();
        }
    }

    public void RemoveClusterProxyConnection(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveClusterProxyConnection(id);
        }
    }

    public FileTransferMethodNoCredentialsExt GetFileTransferMethodById(long fileTransferMethodId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var fileTransferMethod = managementLogic.GetFileTransferMethodById(fileTransferMethodId);
            return fileTransferMethod.ConvertIntToNoCredentialsExt();
        }
    }

    public FileTransferMethodNoCredentialsExt CreateFileTransferMethod(string serverHostname, FileTransferProtocol protocol,
        long clusterId, int? port, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var fileTransferMethod =
                managementLogic.CreateFileTransferMethod(serverHostname, protocol, clusterId, port);
            return fileTransferMethod.ConvertIntToNoCredentialsExt();
        }
    }

    public FileTransferMethodNoCredentialsExt ModifyFileTransferMethod(long id, string serverHostname, FileTransferProtocol protocol,
        long clusterId, int? port, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var fileTransferMethod =
                managementLogic.ModifyFileTransferMethod(id, serverHostname, protocol, clusterId, port);
            return fileTransferMethod.ConvertIntToNoCredentialsExt();
        }
    }

    public void RemoveFileTransferMethod(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveFileTransferMethod(id);
        }
    }

    public ClusterNodeTypeAggregationExt GetClusterNodeTypeAggregationById(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregation = managementLogic.GetClusterNodeTypeAggregationById(id);
            return clusterNodeTypeAggregation.ConvertIntToExt();
        }
    }

    public List<ClusterNodeTypeAggregationExt> GetClusterNodeTypeAggregations(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregation = managementLogic.GetClusterNodeTypeAggregations();
            return clusterNodeTypeAggregation.Select(cna => cna.ConvertIntToExt()).ToList();
        }
    }

    public ClusterNodeTypeAggregationExt CreateClusterNodeTypeAggregation(string name, string description,
        string allocationType, DateTime validityFrom,
        DateTime? validityTo, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregation = managementLogic.CreateClusterNodeTypeAggregation(name, description,
                allocationType, validityFrom,
                validityTo);
            return clusterNodeTypeAggregation.ConvertIntToExt();
        }
    }

    public ClusterNodeTypeAggregationExt ModifyClusterNodeTypeAggregation(long id, string name, string description,
        string allocationType, DateTime validityFrom,
        DateTime? validityTo, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregation = managementLogic.ModifyClusterNodeTypeAggregation(id, name, description,
                allocationType, validityFrom,
                validityTo);
            return clusterNodeTypeAggregation.ConvertIntToExt();
        }
    }

    public void RemoveClusterNodeTypeAggregation(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveClusterNodeTypeAggregation(id);
        }
    }

    public ClusterNodeTypeAggregationAccountingExt GetClusterNodeTypeAggregationAccountingById(
        long clusterNodeTypeAggregationId, long accountingId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregationAccounting =
                managementLogic.GetClusterNodeTypeAggregationAccountingById(clusterNodeTypeAggregationId, accountingId);
            return clusterNodeTypeAggregationAccounting.ConvertIntToExt();
        }
    }

    public ClusterNodeTypeAggregationAccountingExt CreateClusterNodeTypeAggregationAccounting(
        long clusterNodeTypeAggregationId, long accountingId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var clusterNodeTypeAggregationAccounting =
                managementLogic.CreateClusterNodeTypeAggregationAccounting(clusterNodeTypeAggregationId, accountingId);
            return clusterNodeTypeAggregationAccounting.ConvertIntToExt();
        }
    }

    public void RemoveClusterNodeTypeAggregationAccounting(long clusterNodeTypeAggregationId, long accountingId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveClusterNodeTypeAggregationAccounting(clusterNodeTypeAggregationId, accountingId);
        }
    }

    public AccountingExt GetAccountingById(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var accounting = managementLogic.GetAccountingById(id);
            return accounting.ConvertIntToExt();
        }
    }

    public AccountingExt CreateAccounting(string formula, DateTime validityFrom, DateTime? validityTo,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var accounting = managementLogic.CreateAccounting(formula, validityFrom, validityTo);
            return accounting.ConvertIntToExt();
        }
    }

    public AccountingExt ModifyAccounting(long id, string formula, DateTime validityFrom, DateTime? validityTo,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var accounting = managementLogic.ModifyAccounting(id, formula, validityFrom, validityTo);
            return accounting.ConvertIntToExt();
        }
    }

    public void RemoveAccounting(long id, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveAccounting(id);
        }
    }

    public ProjectClusterNodeTypeAggregationExt GetProjectClusterNodeTypeAggregationById(long projectId,
        long clusterNodeTypeAggregationId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var aggregation =
                managementLogic.GetProjectClusterNodeTypeAggregationById(projectId, clusterNodeTypeAggregationId);
            return aggregation.ConvertIntToExt();
        }
    }

    public List<ProjectClusterNodeTypeAggregationExt> GetProjectClusterNodeTypeAggregationsByProjectId(long projectId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var aggregations = managementLogic.GetProjectClusterNodeTypeAggregationsByProjectId(projectId);
            return aggregations.Select(pcna => pcna.ConvertIntToExt()).ToList();
        }
    }

    public ProjectClusterNodeTypeAggregationExt CreateProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var aggregation =
                managementLogic.CreateProjectClusterNodeTypeAggregation(projectId, clusterNodeTypeAggregationId,
                    allocationAmount);
            return aggregation.ConvertIntToExt();
        }
    }

    public ProjectClusterNodeTypeAggregationExt ModifyProjectClusterNodeTypeAggregation(long projectId,
        long clusterNodeTypeAggregationId, long allocationAmount, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            var aggregation =
                managementLogic.ModifyProjectClusterNodeTypeAggregation(projectId, clusterNodeTypeAggregationId,
                    allocationAmount);
            return aggregation.ConvertIntToExt();
        }
    }

    public void RemoveProjectClusterNodeTypeAggregation(long projectId, long clusterNodeTypeAggregationId,
        string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, _) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.ManagementAdmin);
            var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
            managementLogic.RemoveProjectClusterNodeTypeAggregation(projectId, clusterNodeTypeAggregationId);
        }
    }

    #endregion
}
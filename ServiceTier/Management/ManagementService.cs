using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Converts;
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
        public CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId,
            string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    CommandTemplate commandTemplate = managementLogic.CreateCommandTemplate(genericCommandTemplateId, name, projectId, description, code, executableFile, preparationScript);
                    return commandTemplate.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                if (exc.Message.Contains("No such file or directory"))
                {
                    throw new InputValidationException("NoFileOrDirectory");
                }
                throw;
            }
        }

        public CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId,
            string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    CommandTemplate commandTemplate = managementLogic.ModifyCommandTemplate(commandTemplateId, name, projectId, description, code, executableFile, preparationScript);
                    return commandTemplate.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                if (exc.Message.Contains("No such file or directory"))
                {
                    throw new InputValidationException("NoFileOrDirectory");
                }

                throw;
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

                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, commandTemplate.ProjectId.Value);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveCommandTemplate(commandTemplateId);
            }
        }

        public ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedManagementAdminUserForSessionCode(sessionCode, unitOfWork);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Project project = managementLogic.CreateProject(accountingString, usageType, name, description, startDate, endDate, useAccountingStringForScheduler, piEmail,loggedUser);
                return project.ConvertIntToExt();
            }
        }

        public ProjectExt ModifyProject(long id, UsageType usageType, string name, string description, string accountingString, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, id);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                Project project = managementLogic.ModifyProject(id, usageType, name, description, accountingString, startDate, endDate, useAccountingStringForScheduler);
                return project.ConvertIntToExt();
            }
        }

        public void RemoveProject(long id, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, id);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveProject(id);
            }
        }

        public ClusterProjectExt CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProject clusterProject = managementLogic.CreateProjectAssignmentToCluster(projectId, clusterId, localBasepath);
                return clusterProject.ConvertIntToExt();
            }
        }

        public ClusterProjectExt ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                ClusterProject clusterProject = managementLogic.ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);
                return clusterProject.ConvertIntToExt();
            }
        }

        public void RemoveProjectAssignmentToCluster(long projectId, long clusterId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveProjectAssignmentToCluster(projectId, clusterId);
            }
        }

        public List<PublicKeyExt> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.CreateSecureShellKey(credentials, projectId).Select(x=> x.ConvertIntToExt()).ToList();
            }
        }

        public PublicKeyExt RegenerateSecureShellKey(string password, string publicKey, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.RegenerateSecureShellKey(password, publicKey, projectId).ConvertIntToExt();
            }
        }

        public void RemoveSecureShellKey(string publicKey, long projectId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveSecureShellKey(publicKey, projectId);
            }
        }

        public void InitializeClusterScriptDirectory(long projectId, string publicKey, string clusterProjectRootDirectory, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.InitializeClusterScriptDirectory(projectId, publicKey, clusterProjectRootDirectory);
            }
        }

        public bool TestClusterAccessForAccount(long modelProjectId, string modelPublicKey, string modelSessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, UserRoleType.Administrator, modelProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.TestClusterAccessForAccount(modelProjectId, modelPublicKey);
            }
        }
        #endregion
    }
}
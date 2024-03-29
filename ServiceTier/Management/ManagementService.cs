﻿using HEAppE.BusinessLogicTier.Factory;
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
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Maintainer, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate commandTemplate = managementLogic.CreateCommandTemplate(genericCommandTemplateId, name, projectId, description, code, executableFile, preparationScript);
                return commandTemplate.ConvertIntToExt();
            }

        }

        public CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId,
            string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Maintainer, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                CommandTemplate commandTemplate = managementLogic.ModifyCommandTemplate(commandTemplateId, name, projectId, description, code, executableFile, preparationScript);
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

        public void InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.ManagementAdmin, projectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.InitializeClusterScriptDirectory(projectId, clusterProjectRootDirectory);
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
        #endregion
    }
}
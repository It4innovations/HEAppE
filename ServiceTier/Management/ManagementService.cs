using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;
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
        public CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode)
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
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(exc.Message));
                }

                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public string RemoveCommandTemplate(long commandTemplateId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
                    if (commandTemplate == null)
                    {
                        throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
                    }
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, commandTemplate.ProjectId);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    managementLogic.RemoveCommandTemplate(commandTemplateId);
                    return $"CommandTemplate with id {commandTemplateId} has been removed.";
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode)
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
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(exc.Message));
                }

                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ProjectExt CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    Project project = managementLogic.CreateProject(accountingString, usageType, name, description, startDate, endDate, loggedUser);
                    return project.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;

            }
        }

        public ClusterProjectExt AssignProjectToCluster(long projectId, long clusterId, string localBasepath, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, projectId);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    ClusterProject clusterProject = managementLogic.AssignProjectToCluster(projectId, clusterId, localBasepath);
                    return clusterProject.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;

            }
        }

        public PublicKeyExt CreateSecureShellKey(string username, long[] projects, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    return managementLogic.CreateSecureShellKey(username, projects).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public PublicKeyExt RecreateSecureShellKey(string username, string publicKey, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    return managementLogic.RecreateSecureShellKey(username, publicKey).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public string RemoveSecureShellKey(string publicKey, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                    return managementLogic.RemoveSecureShellKey(publicKey);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        #endregion
    }
}

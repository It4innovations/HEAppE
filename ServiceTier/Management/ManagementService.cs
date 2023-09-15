using Exceptions.External;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System;
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
                    throw new InputValidationException("NoFileOrDirectory");
                }

                throw;
            }
        }

        public string RemoveCommandTemplate(long commandTemplateId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
                if (commandTemplate == null)
                {
                    throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, commandTemplate.ProjectId);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                managementLogic.RemoveCommandTemplate(commandTemplateId);
                return $"CommandTemplate with id {commandTemplateId} has been removed.";
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
                    throw new InputValidationException("NoFileOrDirectory");
                }

                throw;
            }
        }

        public PublicKeyExt CreateSecureShellKey(string username, string[] accountingStrings, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.CreateSecureShellKey(username, accountingStrings).ConvertIntToExt();
            }
        }

        public PublicKeyExt RecreateSecureShellKey(string username, string publicKey, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.RecreateSecureShellKey(username, publicKey).ConvertIntToExt();
            }
        }

        public string RemoveSecureShellKey(string publicKey, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                IManagementLogic managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork);
                return managementLogic.RemoveSecureShellKey(publicKey);
            }
        }
        #endregion
    }
}

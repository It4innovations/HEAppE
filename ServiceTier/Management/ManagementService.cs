using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
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
        public CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    CommandTemplate commandTemplate = clusterLogic.CreateCommandTemplate(genericCommandTemplateId, name, description, code, executableFile, preparationScript, loggedUser);
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
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    clusterLogic.RemoveCommandTemplate(commandTemplateId, loggedUser);
                    return $"CommandTemplate with id {commandTemplateId} has been removed.";
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    CommandTemplate commandTemplate = clusterLogic.ModifyCommandTemplate(commandTemplateId, name, description, code, executableFile, preparationScript, loggedUser);
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
        #endregion

    }
}

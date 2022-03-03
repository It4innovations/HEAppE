using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.ClusterInformation.Models;
using log4net;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.ServiceTier.ClusterInformation
{
    public class ClusterInformationService : IClusterInformationService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ClusterExt[] ListAvailableClusters()
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    IList<Cluster> clusters = clusterLogic.ListAvailableClusters();
                    return clusters.Select(s => s.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ClusterNodeUsageExt GetCurrentClusterNodeUsage(long clusterNodeId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    ClusterNodeUsage nodeUsage = clusterLogic.GetCurrentClusterNodeUsage(clusterNodeId, loggedUser);
                    return nodeUsage.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        public IEnumerable<string> GetCommandTemplateParametersName(long commandTemplateId, string userScriptPath, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    return clusterLogic.GetCommandTemplateParametersName(commandTemplateId, userScriptPath, loggedUser);
                }
            }

            catch (Exception exc)
            {
                //TODO Should be rewrite!
                if (exc.Message.Contains("No such file or directory") || exc.Message.Contains("Is a directory"))
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(exc.Message));
                }

                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, string description, string code, string executableFile, string preparationScript, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    CommandTemplate commandTemplate = clusterLogic.CreateCommandTemplate(genericCommandTemplateId, name, description, code, executableFile, preparationScript, loggedUser);
                    return commandTemplate.ConvertIntToExt();
                }
            }

            catch (Exception exc)
            {
                //TODO Should be rewrite!
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
                    //TODO (konvicka): change to non obsolete method
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
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
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    IClusterInformationLogic clusterLogic = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork);
                    CommandTemplate commandTemplate = clusterLogic.ModifyCommandTemplate(commandTemplateId, name, description, code, executableFile, preparationScript, loggedUser);
                    return commandTemplate.ConvertIntToExt();
                }
            }

            catch (Exception exc)
            {
                //TODO Should be rewrite!
                if (exc.Message.Contains("No such file or directory"))
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(exc.Message));
                }

                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
    }
}
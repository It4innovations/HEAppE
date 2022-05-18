using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Reflection;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Exceptions;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.BusinessLogicTier.Logic;

namespace HEAppE.ServiceTier.JobReporting
{
    public class JobReportingService : IJobReportingService
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        private static ILog _logger;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public JobReportingService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        public IEnumerable<AdaptorUserGroupExt> ListAdaptorUserGroups(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Maintainer);
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetAdaptorUserGroups().Select(s=>s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        
        public UserResourceUsageReportExt GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    if (loggedUser.Id != userId)
                    {
                        throw new NotAllowedException("Logged user is not allowed to request this report.");
                    }

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetUserResourceUsageReport(userId, startTime, endTime).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        
        public UserGroupResourceUsageReportExt GetUserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    var group = loggedUser.Groups.FirstOrDefault(val => val.Id == groupId);
                    if (group == null)
                    {
                        throw new NotAllowedException("Logged user is not allowed to request this report.");
                    }

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetUserGroupResourceUsageReport(loggedUser.Id, groupId, startTime, endTime).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
        
        public SubmittedJobInfoUsageReportExt GetResourceUsageReportForJob(long jobId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetResourceUsageReportForJob(jobId).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
    }
}

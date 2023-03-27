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
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;

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
        public IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Maintainer, null);
                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetUserGroupListReport().Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public UserResourceReportExt GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, null);
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

        public UserGroupReportExt GetUserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);
                    var group = loggedUser.Groups.FirstOrDefault(val => val.Id == groupId);
                    if (group == null)
                    {
                        throw new NotAllowedException("Logged user is not allowed to request this report.");
                    }

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return jobReportingLogic.GetUserGroupResourceUsageReport( groupId, startTime, endTime).ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<UserGroupReportExt> GetAggregatedUserGroupResourceUsageReport(DateTime startTime, DateTime endTime, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);

                    IJobReportingLogic jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                    return jobReportingLogic.GetAggregatedUserGroupResourceUsageReport(userGroupIds, startTime, endTime).Select(x => x.ConvertIntToExt()).ToList();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public ProjectReportExt GetResourceUsageReportForJob(long jobId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);
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

        public IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    return reportingLogic.GetAggregatedJobsByStateReport().Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<UserGroupDetailedReportExt> GetJobsDetailedReport(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
                    var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
                    List<long> userGroupIds = loggedUser.Groups.Select(val => val.Id).Distinct().ToList();
                    return reportingLogic.GetJobsDetailedReport(userGroupIds).Select(s => s.ConvertIntToDetailedExt());
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

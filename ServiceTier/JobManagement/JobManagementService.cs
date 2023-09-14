using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ServiceTier.JobManagement
{
    public class JobManagementService : IJobManagementService
    {
        #region Instances
        private readonly ILog _logger;
        #endregion
        #region Constructors
        public JobManagementService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region Methods
        public SubmittedJobInfoExt CreateJob(JobSpecificationByProjectExt specification, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, specification.ProjectId);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    JobSpecification js = specification.ConvertExtToInt(specification.ProjectId);
                    SubmittedJobInfo jobInfo = jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt CreateJob(JobSpecificationByAccountingStringExt specification, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    Project project = unitOfWork.ProjectRepository.GetByAccountingString(specification.AccountingString);
                    if (project == null)
                    {
                        _logger.Error($"Accounting string '{specification.AccountingString}' does not exist in the system.");
                        throw new InputValidationException($"Accounting string '{specification.AccountingString}' does not exist in the system.");
                    }
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    JobSpecification js = specification.ConvertExtToInt(project.Id);
                    SubmittedJobInfo jobInfo = jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ?? throw new InputValidationException($"Job with ID '{createdJobInfoId}' does not exist in the system");
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.ProjectId);
                    SubmittedJobInfo jobInfo = jobLogic.SubmitJob(createdJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException($"Job with ID '{submittedJobInfoId}' does not exist in the system");
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.Project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    SubmittedJobInfo jobInfo = jobLogic.CancelJob(submittedJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public void DeleteJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException($"Job with ID '{submittedJobInfoId}' does not exist in the system");
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.Project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    jobLogic.DeleteJob(submittedJobInfoId, loggedUser);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    var jobInfos = jobLogic.GetJobsForUser(loggedUser);
                    return jobInfos.Select(s => s.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public SubmittedJobInfoExt CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException($"Job with ID '{submittedJobInfoId}' does not exist in the system");
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.Project.Id);

                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    SubmittedJobInfo jobInfo = jobLogic.GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                    return jobInfo.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public void CopyJobDataToTemp(long submittedJobInfoId, string sessionCode, string path)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ?? throw new InputValidationException($"Job with ID '{submittedJobInfoId}' does not exist in the system");
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.Project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);

                    jobLogic.CopyJobDataToTemp(submittedJobInfoId, loggedUser, sessionCode, path);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    JobSpecification job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ?? throw new InputValidationException($"Job with ID '{createdJobInfoId}' does not exist in the system");
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, job.Project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);

                    jobLogic.CopyJobDataFromTemp(createdJobInfoId, loggedUser, tempSessionCode);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var task = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
                    if (task is null)
                    {
                        throw new InputValidationException($"Task with ID '{submittedTaskInfoId}' does not exist in the system");
                    }
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, task.Project.Id);
                    IJobManagementLogic jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork);
                    var nodesIPs = jobLogic.GetAllocatedNodesIPs(submittedTaskInfoId, loggedUser);

                    return nodesIPs.ToArray();
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
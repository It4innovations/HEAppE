using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using SshCaAPI;

namespace HEAppE.ServiceTier.JobManagement;

public class JobManagementService : IJobManagementService
{
    #region Instances

    private readonly ILog _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;

    #endregion

    #region Constructors

    public JobManagementService(ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
    }

    #endregion

    #region Methods

    public SubmittedJobInfoExt CreateJob(JobSpecificationExt specification, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, specification.ProjectId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            SubProject subProject = null;

            if (!string.IsNullOrEmpty(specification.SubProjectIdentifier))
            {
                var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService);
                subProject =
                    managementLogic.CreateSubProject(specification.SubProjectIdentifier, specification.ProjectId);
            }

            var js = specification.ConvertExtToInt(specification.ProjectId, subProject?.Id);
            var jobInfo = jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
            return jobInfo.ConvertIntToExt();
        }
    }

    public SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.ProjectId);
            var jobInfo = jobLogic.SubmitJob(createdJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public SubmittedJobInfoExt CancelJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            var jobInfo = jobLogic.CancelJob(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public bool DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            if (archiveLogs)
            {
                _logger.Info($"Archiving job logs {submittedJobInfoId} by user {loggedUser.Id}");
                jobLogic.ArchiveJob(submittedJobInfoId, loggedUser);
            }
            return jobLogic.DeleteJob(submittedJobInfoId, loggedUser);
        }
    }

    public SubmittedJobInfoExt[] ListJobsForCurrentUser(string sessionCode, string jobStates = null)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                    AdaptorUserRoleType.Submitter);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            var jobInfos = jobLogic.GetJobsForUser(loggedUser);
            var result = jobInfos.Select(s => s.ConvertIntToExt()).ToArray();
            if (jobStates != null)
            {
                var values = Enum.GetValues(typeof(JobStateExt));
                Array.Reverse(values);
                foreach (JobStateExt st in values) jobStates = jobStates.Replace(((int)st).ToString(), st.ToString());
                jobStates = Regex.Replace(jobStates.Replace(" ", ""), @",+", ",").Trim(',');
                if (!Enum.TryParse(jobStates, true, out JobStateExt statesComb))
                    throw new InputValidationException("JobStatesInvalid", jobStates);
                result = result.Where(x => ((int)statesComb & (int)x.State) != 0).ToArray();
            }

            return result;
        }
    }

    public SubmittedJobInfoExt CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.Project.Id);

            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            var jobInfo = jobLogic.GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public void CopyJobDataToTemp(long createdJobInfoId, string sessionCode, string path)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);

            jobLogic.CopyJobDataToTemp(createdJobInfoId, loggedUser, sessionCode, path);
        }
    }

    public void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);

            jobLogic.CopyJobDataFromTemp(createdJobInfoId, loggedUser, tempSessionCode);
        }
    }

    public IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var task = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
            if (task is null) throw new InputValidationException("NotExistingTask", submittedTaskInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, task.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _sshCertificateAuthorityService);
            var nodesIPs = jobLogic.GetAllocatedNodesIPs(submittedTaskInfoId, loggedUser);

            return nodesIPs.ToArray();
        }
    }

    #endregion
}
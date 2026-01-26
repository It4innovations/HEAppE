using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using Microsoft.EntityFrameworkCore;
using SshCaAPI;

namespace HEAppE.ServiceTier.JobManagement;

public class JobManagementService : IJobManagementService
{
    #region Instances

    private readonly ILog _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;

    #endregion

    #region Constructors

    public JobManagementService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _userOrgService = userOrgService;
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
    }

    #endregion

    #region Methods

    public async Task<SubmittedJobInfoExt> CreateJob(JobSpecificationExt specification, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, specification.ProjectId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            SubProject subProject = null;

            if (!string.IsNullOrEmpty(specification.SubProjectIdentifier))
            {
                var managementLogic = LogicFactory.GetLogicFactory().CreateManagementLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
                subProject =
                    managementLogic.CreateSubProject(specification.SubProjectIdentifier, specification.ProjectId);
            }

            var js = specification.ConvertExtToInt(specification.ProjectId, subProject?.Id);
            var jobInfo = await jobLogic.CreateJob(js, loggedUser, specification.IsExtraLong);
            return jobInfo.ConvertIntToExt();
        }
    }

    public SubmittedJobInfoExt SubmitJob(long createdJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.ProjectId);
            var jobInfo = jobLogic.SubmitJob(createdJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> GetActualTasksInfo(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            var jobInfo =  await jobLogic.GetActualTasksInfo(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public async Task<SubmittedJobInfoExt> CancelJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            var jobInfo = await jobLogic.CancelJob(submittedJobInfoId, loggedUser);
            return jobInfo.ConvertIntToExt();
        }
    }

    public bool DeleteJob(long submittedJobInfoId, bool archiveLogs, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
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
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();

        var (loggedUser, _) = UserAndLimitationManagementService.GetValidatedUserForSessionCode(
            sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, AdaptorUserRoleType.Submitter);

        var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(
            unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);


        IQueryable<SubmittedJobInfo> query = jobLogic.GetJobsForUserQuery(loggedUser.Id)
            .AsNoTracking()
            .Include(x => x.Specification) // This is for the Job
            .Include(x => x.Project)       // This is for the Job
            .Include(x => x.Tasks)
            .ThenInclude(t => t.NodeType)
            .Include(x => x.Tasks) 
            .ThenInclude(t => t.Project)
            .Include(x => x.Tasks)        
            .ThenInclude(t => t.Specification); // Load Specification for each Task
        
        if (!string.IsNullOrWhiteSpace(jobStates))
        {
            var stateInts = jobStates.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim()))
                .ToList();
            
            query = query.Where(x => stateInts.Contains((int)x.State));
        }

        return query
            .ToList() 
            .Select(x => x.ConvertIntToExt())
            .ToArray();
    }


    public async Task<SubmittedJobInfoExt> CurrentInfoForJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", submittedJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);

            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            if (JwtTokenIntrospectionConfiguration.IsEnabled)
            {
                var jobInfoFromHPC = await jobLogic.GetActualTasksInfo(submittedJobInfoId, loggedUser);
                return jobInfoFromHPC.ConvertIntToExt();
            }
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
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);

            jobLogic.CopyJobDataToTemp(createdJobInfoId, loggedUser, sessionCode, path);
        }
    }

    public void CopyJobDataFromTemp(long createdJobInfoId, string sessionCode, string tempSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, job.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);

            jobLogic.CopyJobDataFromTemp(createdJobInfoId, loggedUser, tempSessionCode);
        }
    }

    public IEnumerable<string> AllocatedNodesIPs(long submittedTaskInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var task = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
            if (task is null) throw new InputValidationException("NotExistingTask", submittedTaskInfoId);
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, task.Project.Id);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            var nodesIPs = jobLogic.GetAllocatedNodesIPs(submittedTaskInfoId, loggedUser);

            return nodesIPs.ToArray();
        }
    }

    public async Task<DryRunJobInfoExt> DryRunJob(long modelProjectId, long modelClusterNodeTypeId, long modelNodes,
        long modelTasksPerNode,
        long modelWallTimeInMinutes, string modelSessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(modelSessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                AdaptorUserRoleType.Submitter, modelProjectId);
            var jobLogic = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            var dryRunResult = (await jobLogic.DryRunJob(modelProjectId, modelClusterNodeTypeId, modelNodes,
                modelTasksPerNode, modelWallTimeInMinutes, loggedUser)).ConvertIntToExt();
            return dryRunResult;
        }
    }

    #endregion
}
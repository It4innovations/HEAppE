using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobReporting.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using SshCaAPI;

namespace HEAppE.ServiceTier.JobReporting;

public class JobReportingService : IJobReportingService
{
    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    private static ILog _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public JobReportingService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = httpContextKeys ?? throw new ArgumentNullException(nameof(httpContextKeys));
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #endregion

    public IEnumerable<UserGroupListReportExt> ListAdaptorUserGroups(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.GroupReporter);
            var jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            return jobReportingLogic.UserGroupListReport(projects, loggedUser.Id)
                .Where(s => s != null)
                .Select(s => s.ConvertIntToExt());
        }
    }

    public IEnumerable<ProjectReportExt> UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime,
        string[] subProjects, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.GroupReporter); 
            var jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            var projectIds = projects.Select(p => p.Id).ToList();
            var reporterGroups = loggedUser.Groups
                .Where(g => projectIds.Contains(g.ProjectId ?? 0))
                .Select(g => g.Id)
                .Distinct()
                .ToList();
            return jobReportingLogic.UserResourceUsageReport(userId, reporterGroups, startTime, endTime, subProjects)
                .Where(s => s != null)
                .Select(g => g.ConvertIntToExt());
        }
    }

    public ProjectReportExt UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime,
        string[] subProjects, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.Reporter);
            var group = loggedUser.Groups.FirstOrDefault(val => val.Id == groupId);
            if (group == null) throw new NotAllowedException("NotAllowedToRequestReport");

            if (!projects.Any(x => x.Id == group.ProjectId)) throw new NotAllowedException("NotAllowedToRequestReport");
            var jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            return jobReportingLogic.UserGroupResourceUsageReport(groupId, startTime, endTime, subProjects)
                .ConvertIntToExt();
        }
    }

    public IEnumerable<ProjectAggregatedReportExt> AggregatedUserGroupResourceUsageReport(DateTime startTime,
        DateTime endTime, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.Reporter);

            var jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            var userGroupIds = loggedUser.Groups.Select(x => x.Id).Distinct().ToList();

            //get only groups which are in projects which are allowed for logged user
            var reportAllowedGroupIds = userGroupIds.Where(gId => 
            {
                var userGroup = loggedUser.Groups.FirstOrDefault(ug => ug.Id == gId);
                if (userGroup == null) return false;

                if (!userGroup.ProjectId.HasValue) return true; 

                return projects.Any(p => p.Id == userGroup.ProjectId);
            }).ToList();

            return jobReportingLogic.AggregatedUserGroupResourceUsageReport(reportAllowedGroupIds, startTime, endTime)
                .Where(s => s != null)
                .Select(x => x.ConvertIntToExt()).ToList();
        }
    }

    public ProjectExtendedReportExt ResourceUsageReportForJob(long jobId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.Reporter);
            var jobReportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            var projectIds = projects.Select(p => p.Id).ToList();
            var userGroupIds = loggedUser.Groups
                .Where(x => projectIds.Contains(x.ProjectId ?? 0))
                .ToList()
                .Select(val => val.Id).Distinct().ToList();
            return jobReportingLogic.ResourceUsageReportForJob(jobId, userGroupIds).ConvertIntToExtendedExt();
        }
    }

    public IEnumerable<JobStateAggregationReportExt> GetJobsStateAgregationReport(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.GroupReporter);
            var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);
            return reportingLogic.AggregatedJobsByStateReport(projects).Select(s => s.ConvertIntToExt());
        }
    }

    public IEnumerable<ProjectDetailedReportExt> JobsDetailedReport(string[] subProjects, DateTime? timeFrom, DateTime? timeTo, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            (var loggedUser, var projects) =
                UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys,
                    AdaptorUserRoleType.GroupReporter);

            var reportingLogic = LogicFactory.GetLogicFactory().CreateJobReportingLogic(unitOfWork);

            var allowedProjectIds = projects != null 
                ? new HashSet<long>(projects.Where(p => p != null).Select(p => p.Id)) 
                : new HashSet<long>();

            var reportAllowedGroupIds = loggedUser?.Groups != null
                ? loggedUser.Groups
                    .Where(g => g != null && g.ProjectId.HasValue && allowedProjectIds.Contains(g.ProjectId.Value))
                    .Select(g => g.Id)
                    .Distinct()
                    .ToList()
                : new List<long>();

            var reports = reportingLogic.JobsDetailedReport(reportAllowedGroupIds, subProjects, timeFrom, timeTo);

            if (reports == null)
                return Enumerable.Empty<ProjectDetailedReportExt>();

            return reports
                .Where(s => s != null)
                .Select(s => s.ConvertIntToDetailedExt())
                .ToList();
        }
    }
}
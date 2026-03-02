using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Converts;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using log4net;
using Project = HEAppE.DomainObjects.JobManagement.Project;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting;

internal class JobReportingLogic : IJobReportingLogic
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly ILog _log;

    internal JobReportingLogic(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    public IEnumerable<UserGroupListReport> UserGroupListReport(IEnumerable<Project> projects, long userId)
    {
        var enumerable = projects as Project[] ?? projects.ToArray();
        var projectIds = enumerable?.Select(p => p.Id).ToList() ?? new List<long>();
        if (!projectIds.Any()) return Enumerable.Empty<UserGroupListReport>();
        
        var groupsRaw = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .Include(x => x.AdaptorUserUserGroupRoles).ThenInclude(r => r.AdaptorUserRole)
            .Where(x => x.ProjectId.HasValue && projectIds.Contains(x.ProjectId.Value))
            .Where(x => x.AdaptorUserUserGroupRoles.Any(y => y.AdaptorUserId == userId))
            .ToList();
        
        var filteredGroups = groupsRaw.Where(g => g.AdaptorUserUserGroupRoles.Any(y => 
                y.AdaptorUserId == userId && 
                y.AdaptorUserRole != null && 
                y.AdaptorUserRole.ContainedRoleTypes.Any(a => a == AdaptorUserRoleType.GroupReporter)))
            .DistinctBy(g => g.Project?.Id)
            .ToList();
        
        var pIds = filteredGroups.Where(g => g.Project != null).Select(g => g.Project.Id).Distinct().ToList();
        var subProjects = enumerable.Where(p => p.SubProjects != null).SelectMany(p => p.SubProjects).Select(sp => sp.Identifier).ToArray();
        var jobsLookup = GetJobsLookup(pIds, DateTime.MinValue, DateTime.UtcNow, subProjects);

        return filteredGroups.Select(g => new UserGroupListReport
        {
            AdaptorUserGroup = g,
            Project = BuildProjectReport(g.Project, (g.Project != null && jobsLookup.Contains(g.Project.Id)) ? jobsLookup[g.Project.Id] : Enumerable.Empty<SubmittedJobInfo>()),
            UsageType = g.Project?.UsageType ?? 0
        }).ToList();
    }
public ProjectReport ResourceUsageReportForJob(long jobId, IEnumerable<long> reporterGroupIds)
    {
        var jobData = _unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
            .Include(j => j.Submitter)
            .Include(j => j.Project).ThenInclude(p => p.AdaptorUserGroups)
            .Include(j => j.Specification).ThenInclude(s => s.SubProject)
            .Include(j => j.Tasks).ThenInclude(t => t.NodeType).ThenInclude(nt => nt.Cluster)
            .Include(j => j.Tasks).ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks).ThenInclude(t => t.Specification).ThenInclude(s => s.CommandTemplate)
            .AsNoTracking()
            .Where(j => j.Id == jobId)
            .Select(j => new {
                Job = j,
                Submitter = j.Submitter,
                Project = j.Project,
                Spec = j.Specification,
                SubProj = j.Specification != null ? j.Specification.SubProject : null,
                Tasks = j.Tasks.Select(t => new {
                    t,
                    nt = t.NodeType,
                    c = t.NodeType != null ? t.NodeType.Cluster : null,
                    rc = t.ResourceConsumed,
                    tspec = t.Specification,
                    tmpl = t.Specification != null ? t.Specification.CommandTemplate : null
                }).ToList()
            })
            .FirstOrDefault() ?? throw new ResourceUsageException("JobNotSpecified", jobId);

        var projectGroups = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .Where(g => g.Project != null && g.ProjectId == jobData.Project.Id).ToList();
        var rGroupIds = reporterGroupIds?.ToList() ?? new List<long>();
        
        if (jobData.Project == null || !projectGroups.Any())
            throw new ResourceUsageException("ReporterNoAccessToJob", jobId);
        
        var job = jobData.Job;
        job.Project = jobData.Project;
        job.Submitter = jobData.Submitter;
        job.Specification = jobData.Spec;
        if (job.Specification != null) job.Specification.SubProject = jobData.SubProj;
        
        job.Tasks = jobData.Tasks.Select(x => {
            x.t.NodeType = x.nt;
            if (x.t.NodeType != null) x.t.NodeType.Cluster = x.c;
            x.t.ResourceConsumed = x.rc;
            x.t.Specification = x.tspec;
            if (x.t.Specification != null) x.t.Specification.CommandTemplate = x.tmpl;
            return x.t;
        }).ToList();

        return new ProjectReport { Clusters = GetClusterReportsForJob(job), Project = job.Project };
    }

    public IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport(IEnumerable<Project> projects)
    {
        var ids = projects?.Select(p => p.Id).ToList();
        if (ids == null || !ids.Any()) return Enumerable.Empty<JobStateAggregationReport>();

        return _unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .Where(x => x.Project != null && ids.Contains(x.Project.Id))
            .GroupBy(g => g.State)
            .Select(s => new JobStateAggregationReport { State = s.Key, Count = s.Count() })
            .ToList();
    }

    public IEnumerable<ProjectReport> JobsDetailedReport(IEnumerable<long> groupIds, string[] subProjects, DateTime? timeFrom, DateTime? timeTo)
    {
        var ids = groupIds?.ToList();
        if (ids == null || !ids.Any()) return Enumerable.Empty<ProjectReport>();

        var groups = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .Where(g => ids.Contains(g.Id))
            .ToList();

        var pIds = groups.Where(g => g.Project != null).Select(g => g.Project.Id).Distinct().ToList();
        var jobsLookup = GetJobsLookup(pIds, timeFrom ?? DateTime.MinValue, timeTo ?? DateTime.UtcNow, subProjects);

        return groups.Select(g => BuildProjectReport(g.Project, (g.Project != null && jobsLookup.Contains(g.Project.Id)) ? jobsLookup[g.Project.Id] : Enumerable.Empty<SubmittedJobInfo>()))
                     .Where(r => r != null).ToList();
    }

    public IEnumerable<ProjectReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds, DateTime startTime, DateTime endTime, string[] subProjects)
    {
        var userGroupIds = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .Where(g => g.AdaptorUserUserGroupRoles.Any(r => r.AdaptorUserId == userId))
            .Select(g => g.Id)
            .ToList();

        if (!userGroupIds.Any()) throw new ResourceUsageException("UserNotSpecified", userId);

        var targetGroupIds = (reporterGroupIds ?? Enumerable.Empty<long>()).Intersect(userGroupIds).ToList();

        return JobsDetailedReport(targetGroupIds, subProjects, startTime, endTime);
    }

    public ProjectReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string[] subProjects)
    {
        return JobsDetailedReport(new[] { groupId }, subProjects, startTime, endTime).FirstOrDefault();
    }

    public ProjectAggregatedReport UserGroupResourceAggregatedUsageReport(long groupId, DateTime startTime, DateTime endTime)
    {
        var group = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Project).ThenInclude(p => p.SubProjects)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .FirstOrDefault(g => g.Id == groupId) ?? throw new ResourceUsageException("GroupNotSpecified", groupId);

        if (group.Project == null) return null;

        var jobs = GetJobsLookup(new[] { group.Project.Id }, startTime, endTime, null)[group.Project.Id].ToList();

        return new ProjectAggregatedReport
        {
            Project = group.Project,
            SubProjects = group.Project.SubProjects?.Select(sp => new SubProjectAggregatedReport {
                SubProject = sp,
                Clusters = BuildClusterAggregatedReports(group.Project, jobs.Where(j => j.Specification?.SubProjectId == sp.Id))
            }).ToList() ?? new List<SubProjectAggregatedReport>(),
            Clusters = BuildClusterAggregatedReports(group.Project, jobs)
        };
    }

    public IEnumerable<ProjectAggregatedReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime)
    {
        return (groupIds?.ToList() ?? new List<long>())
            .Select(id => UserGroupResourceAggregatedUsageReport(id, startTime, endTime))
            .Where(r => r != null).ToList();
    }

    private ILookup<long, SubmittedJobInfo> GetJobsLookup(IEnumerable<long> projectIds, DateTime start, DateTime end, string[] subProjects)
    {
        var pIds = projectIds?.ToList();
        if (pIds == null || !pIds.Any()) return Enumerable.Empty<SubmittedJobInfo>().ToLookup(x => 0L);

        var query = _unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Submitter)
            .Include(j => j.Project)
            .Include(j => j.Specification).ThenInclude(s => s.SubProject)
            .Include(j => j.Tasks).ThenInclude(t => t.NodeType)
            .Include(j => j.Tasks).ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks).ThenInclude(t => t.Specification).ThenInclude(s => s.CommandTemplate)
            .Where(j => j.Project != null && pIds.Contains(j.Project.Id))
            .Where(j => j.StartTime >= start && j.EndTime <= end);

        if (subProjects?.Any() == true)
            query = query.Where(j => j.Specification != null && j.Specification.SubProject != null && subProjects.Contains(j.Specification.SubProject.Identifier));

        return query.ToList().ToLookup(j => j.Project.Id);
    }

    private ProjectReport BuildProjectReport(Project project, IEnumerable<SubmittedJobInfo> jobs)
    {
        if (project == null) return null;
        var jobsList = jobs?.ToList() ?? new List<SubmittedJobInfo>();

        return new ProjectReport {
            Project = project,
            Clusters = project.ClusterProjects?.Select(cp => cp.Cluster).Where(c => c != null).Distinct().Select(cluster => new ClusterReport {
                Cluster = cluster,
                ClusterNodeTypes = cluster.NodeTypes?.Select(nt => new ClusterNodeTypeReport {
                    ClusterNodeType = nt,
                    Jobs = jobsList.Where(j => j.Tasks != null && j.Tasks.Any(t => t.NodeType != null && t.NodeType.Id == nt.Id))
                        .Select(j => new JobReport {
                            SubmittedJobInfo = j,
                            Tasks = j.Tasks.Where(t => t.NodeType != null && t.NodeType.Id == nt.Id).Select(t => new TaskReport {
                                SubmittedTaskInfo = t,
                                Usage = Math.Round(t.ResourceConsumed?.Value ?? 0, 3)
                            }).ToList()
                        }).ToList()
                }).ToList() ?? new List<ClusterNodeTypeReport>()
            }).ToList() ?? new List<ClusterReport>()
        };
    }

    private List<ClusterAggregatedReport> BuildClusterAggregatedReports(Project project, IEnumerable<SubmittedJobInfo> jobs)
    {
        var jobsList = jobs?.ToList() ?? new List<SubmittedJobInfo>();
        return project.ClusterProjects?.Select(cp => cp.Cluster).Where(c => c != null).Distinct().Select(cluster => new ClusterAggregatedReport {
            Cluster = cluster,
            ClusterNodeTypesAggregations = cluster.NodeTypes?
                .GroupBy(nt => nt.ClusterNodeTypeAggregation)
                .Select(g => new ClusterNodeTypeAggregatedReport {
                    ClusterNodeTypeAggregation = g.Key,
                    ClusterNodeTypes = g.Select(nt => new ClusterNodeTypeReport {
                        ClusterNodeType = nt,
                        Jobs = jobsList.Where(j => j.Tasks != null && j.Tasks.Any(t => t.NodeType != null && t.NodeType.Id == nt.Id))
                            .Select(j => new JobReport {
                                SubmittedJobInfo = j,
                                Tasks = j.Tasks.Where(t => t.NodeType != null && t.NodeType.Id == nt.Id).Select(t => new TaskReport {
                                    SubmittedTaskInfo = t,
                                    Usage = Math.Round(t.ResourceConsumed?.Value ?? 0, 3)
                                }).ToList()
                            }).ToList()
                    }).ToList()
                }).ToList() ?? new List<ClusterNodeTypeAggregatedReport>()
        }).ToList() ?? new List<ClusterAggregatedReport>();
    }

    private List<ClusterReport> GetClusterReportsForJob(SubmittedJobInfo job)
    {
        if (job?.Tasks == null) return new List<ClusterReport>();
        return job.Tasks.Where(t => t.NodeType?.Cluster != null).GroupBy(t => t.NodeType.Cluster).Select(cg => new ClusterReport {
            Cluster = cg.Key,
            ClusterNodeTypes = cg.GroupBy(t => t.NodeType).Select(ng => new ClusterNodeTypeReport {
                ClusterNodeType = ng.Key,
                Jobs = new List<JobReport> { new JobReport { 
                    SubmittedJobInfo = job, 
                    Tasks = ng.Select(t => new TaskReport { 
                        SubmittedTaskInfo = t, 
                        Usage = Math.Round(t.ResourceConsumed?.Value ?? 0, 3)
                    }).ToList() 
                } }
            }).ToList()
        }).ToList();
    }
}
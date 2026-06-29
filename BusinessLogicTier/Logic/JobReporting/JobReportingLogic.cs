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
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using Microsoft.Extensions.Logging;
using Project = HEAppE.DomainObjects.JobManagement.Project;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting;

internal class JobReportingLogic : IJobReportingLogic
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly ILogger _logger;

    internal JobReportingLogic(IUnitOfWork unitOfWork, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            .Include(x => x.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.FileTransferMethods)
            .Include(x => x.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.ProxyConnection)
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
        var jobsLookup = GetJobsLookup(pIds, DateTime.UtcNow.AddDays(-90), DateTime.UtcNow, subProjects);

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
            .Include(j => j.Tasks).ThenInclude(t => t.Specification)
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
                    tspec = t.Specification
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
            return x.t;
        }).ToList();

        // Re-attach CommandTemplates including soft-deleted ones so historical data is always visible
        AttachCommandTemplatesIncludingDeleted(job.Tasks);

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

    public IEnumerable<ProjectReport> JobsDetailedReport(IEnumerable<long> groupIds, string[] subProjects, DateTime? timeFrom, DateTime? timeTo,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null)
    {
        var ids = groupIds?.ToList();
        if (ids == null || !ids.Any()) return Enumerable.Empty<ProjectReport>();

        var groups = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.FileTransferMethods)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.ProxyConnection)
            .Where(g => ids.Contains(g.Id))
            .ToList();

        var pIds = groups.Where(g => g.Project != null).Select(g => g.Project.Id).Distinct().ToList();
        var jobsLookup = GetJobsLookup(pIds, timeFrom ?? DateTime.MinValue, timeTo ?? DateTime.UtcNow, subProjects, limit, offset, clusterId, userId);

        return groups.Select(g => BuildProjectReport(g.Project, (g.Project != null && jobsLookup.Contains(g.Project.Id)) ? jobsLookup[g.Project.Id] : Enumerable.Empty<SubmittedJobInfo>()))
                     .Where(r => r != null).ToList();
    }

    public IEnumerable<ProjectReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds, DateTime startTime, DateTime endTime, string[] subProjects,
        int? limit = null, int? offset = null, long? clusterId = null)
    {
        var userGroupIds = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .Where(g => g.AdaptorUserUserGroupRoles.Any(r => r.AdaptorUserId == userId))
            .Select(g => g.Id)
            .ToList();

        if (!userGroupIds.Any()) throw new ResourceUsageException("UserNotSpecified", userId);

        var targetGroupIds = (reporterGroupIds ?? Enumerable.Empty<long>()).Intersect(userGroupIds).ToList();

        return JobsDetailedReport(targetGroupIds, subProjects, startTime, endTime, limit, offset, clusterId, userId);
    }

    public ProjectReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime, string[] subProjects,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null)
    {
        return JobsDetailedReport(new[] { groupId }, subProjects, startTime, endTime, limit, offset, clusterId, userId).FirstOrDefault();
    }

    public ProjectAggregatedReport UserGroupResourceAggregatedUsageReport(long groupId, DateTime startTime, DateTime endTime,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null)
    {
        var group = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Project).ThenInclude(p => p.SubProjects)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.FileTransferMethods)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.ProxyConnection)
            .FirstOrDefault(g => g.Id == groupId) ?? throw new ResourceUsageException("GroupNotSpecified", groupId);

        if (group.Project == null) return null;

        var jobs = GetJobsLookup(new[] { group.Project.Id }, startTime, endTime, null, limit, offset, clusterId, userId)[group.Project.Id].ToList();

        var subProjectsReports = group.Project.SubProjects?.Select(sp => new SubProjectAggregatedReport {
            SubProject = sp,
            Clusters = BuildClusterAggregatedReports(group.Project, jobs.Where(j => j.Specification?.SubProjectId == sp.Id))
        }).ToList() ?? new List<SubProjectAggregatedReport>();

        // Check if there are jobs without subproject and non-zero usage
        var jobsWithoutSubProject = jobs.Where(j => j.Specification?.SubProjectId == null).ToList();
        var unassignedClusters = BuildClusterAggregatedReports(group.Project, jobsWithoutSubProject);
        if (unassignedClusters.Any(c => c.TotalUsage > 0))
        {
            subProjectsReports.Add(new SubProjectAggregatedReport {
                SubProject = new SubProject { Identifier = null },
                Clusters = unassignedClusters
            });
        }

        return new ProjectAggregatedReport
        {
            Project = group.Project,
            SubProjects = subProjectsReports,
            Clusters = BuildClusterAggregatedReports(group.Project, jobs)
        };
    }

    public IEnumerable<ProjectAggregatedReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null)
    {
        var groupIdsList = groupIds?.ToList() ?? new List<long>();
        if (!groupIdsList.Any()) return Enumerable.Empty<ProjectAggregatedReport>();

        var groups = _unitOfWork.AdaptorUserGroupRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Project).ThenInclude(p => p.SubProjects)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.NodeTypes)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.FileTransferMethods)
            .Include(g => g.Project).ThenInclude(p => p.ClusterProjects).ThenInclude(cp => cp.Cluster).ThenInclude(c => c.ProxyConnection)
            .Where(g => groupIdsList.Contains(g.Id))
            .ToList();

        // Group by project ID to avoid duplicates in the response
        return groups
            .Where(g => g.ProjectId.HasValue && g.Project != null)
            .DistinctBy(g => g.ProjectId.Value)
            .Select(g => UserGroupResourceAggregatedUsageReport(g.Id, startTime, endTime, limit, offset, clusterId, userId))
            .Where(r => r != null).ToList();
    }

    private ILookup<long, SubmittedJobInfo> GetJobsLookup(IEnumerable<long> projectIds, DateTime start, DateTime end, string[] subProjects,
        int? limit = null, int? offset = null, long? clusterId = null, long? userId = null)
    {
        var pIds = projectIds?.ToList();
        if (pIds == null || !pIds.Any()) return Enumerable.Empty<SubmittedJobInfo>().ToLookup(x => 0L);

        string timezone = null;
        if (clusterId.HasValue)
        {
            var cluster = _unitOfWork.ClusterRepository.GetById(clusterId.Value);
            timezone = cluster?.TimeZone;
        }

        if (timezone == null)
        {
            var firstProjectId = pIds.FirstOrDefault();
            if (firstProjectId > 0)
            {
                var project = _unitOfWork.ProjectRepository.GetByIdWithClusterProjects(firstProjectId);
                timezone = project?.ClusterProjects?.FirstOrDefault()?.Cluster?.TimeZone;
            }
        }

        if (!string.IsNullOrEmpty(timezone))
        {
            if (start != DateTime.MinValue && start.Kind != DateTimeKind.Utc)
                start = HEAppE.Utils.DateTimeZoneExtension.Convert(start, timezone);
            if (end != DateTime.MaxValue && end.Kind != DateTimeKind.Utc)
                end = HEAppE.Utils.DateTimeZoneExtension.Convert(end, timezone);
        }

        var query = _unitOfWork.SubmittedJobInfoRepository.GetQueryableWithoutFilters()
            .AsNoTracking()
            .Where(j => j.Project != null && pIds.Contains(j.Project.Id))
            .Where(j => j.StartTime >= start && j.EndTime <= end);

        if (subProjects?.Any() == true)
            query = query.Where(j => j.Specification != null && j.Specification.SubProject != null && subProjects.Contains(j.Specification.SubProject.Identifier));

        if (userId.HasValue)
            query = query.Where(j => j.Submitter != null && j.Submitter.Id == userId.Value);

        if (clusterId.HasValue)
            query = query.Where(j => j.Tasks.Any(t => t.NodeType != null && t.NodeType.Cluster != null && t.NodeType.Cluster.Id == clusterId.Value));

        query = query.OrderByDescending(j => j.Id);

        if (offset.HasValue)
            query = query.Skip(offset.Value);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        var jobsData = query.Select(j => new {
            j.Id,
            j.Name,
            j.State,
            j.CreationTime,
            j.StartTime,
            j.SubmitTime,
            j.EndTime,
            ProjectId = j.Project != null ? (long?)j.Project.Id : null,
            SubmitterUsername = j.Submitter != null ? j.Submitter.Username : null,
            SpecificationSubProjectId = j.Specification != null ? j.Specification.SubProjectId : null,
            SpecificationSubProjectIdentifier = (j.Specification != null && j.Specification.SubProject != null) ? j.Specification.SubProject.Identifier : null,
            Tasks = j.Tasks.Select(t => new {
                t.Id,
                t.ScheduledJobId,
                t.Name,
                t.StartTime,
                t.EndTime,
                t.State,
                NodeTypeId = t.NodeType != null ? (long?)t.NodeType.Id : null,
                NodeTypeName = t.NodeType != null ? t.NodeType.Name : null,
                ResourceConsumedValue = t.ResourceConsumed != null ? t.ResourceConsumed.Value : null,
                CommandTemplateId = t.Specification != null ? (long?)t.Specification.CommandTemplateId : null,
                CommandTemplateName = (t.Specification != null && t.Specification.CommandTemplate != null) ? t.Specification.CommandTemplate.Name : null
            }).ToList()
        }).ToList();

        var jobs = jobsData.Select(d => {
            var job = new SubmittedJobInfo
            {
                Id = d.Id,
                Name = d.Name,
                State = d.State,
                CreationTime = d.CreationTime,
                StartTime = d.StartTime,
                SubmitTime = d.SubmitTime,
                EndTime = d.EndTime,
                Project = d.ProjectId.HasValue ? new Project { Id = d.ProjectId.Value } : null,
                Submitter = d.SubmitterUsername != null ? new AdaptorUser { Username = d.SubmitterUsername } : null,
                Specification = new JobSpecification
                {
                    SubProjectId = d.SpecificationSubProjectId,
                    SubProject = d.SpecificationSubProjectIdentifier != null ? new SubProject
                    {
                        Id = d.SpecificationSubProjectId ?? 0,
                        Identifier = d.SpecificationSubProjectIdentifier
                    } : null
                }
            };
            job.Tasks = d.Tasks.Select(t => new SubmittedTaskInfo
            {
                Id = t.Id,
                ScheduledJobId = t.ScheduledJobId,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                State = t.State,
                NodeType = t.NodeTypeId.HasValue ? new ClusterNodeType { Id = t.NodeTypeId.Value, Name = t.NodeTypeName } : null,
                ResourceConsumed = t.ResourceConsumedValue.HasValue ? new ResourceConsumed { Value = t.ResourceConsumedValue.Value } : null,
                Specification = t.CommandTemplateId.HasValue ? new TaskSpecification
                {
                    CommandTemplateId = t.CommandTemplateId.Value,
                    CommandTemplate = t.CommandTemplateName != null ? new CommandTemplate { Id = t.CommandTemplateId.Value, Name = t.CommandTemplateName } : null
                } : null
            }).ToList();
            return job;
        }).ToList();

        // Re-attach CommandTemplates including soft-deleted ones so historical job data
        // always shows the template info even if the template was deleted after the job ran.
        AttachCommandTemplatesIncludingDeleted(jobs.SelectMany(j => j.Tasks ?? Enumerable.Empty<SubmittedTaskInfo>()));

        return jobs.ToLookup(j => j.Project?.Id ?? 0L);
    }

    /// <summary>
    /// After loading tasks from EF (where global soft-delete filter blanks out deleted CommandTemplates),
    /// fetch any missing templates directly bypassing the filter and assign them back.
    /// </summary>
    private void AttachCommandTemplatesIncludingDeleted(IEnumerable<SubmittedTaskInfo> tasks)
    {
        var taskList = tasks?.ToList();
        if (taskList == null || !taskList.Any()) return;

        var missingTemplateIds = taskList
            .Where(t => t.Specification != null && t.Specification.CommandTemplate == null && t.Specification.CommandTemplateId > 0)
            .Select(t => t.Specification.CommandTemplateId)
            .Distinct()
            .ToList();

        if (!missingTemplateIds.Any()) return;

        var templates = _unitOfWork.CommandTemplateRepository
            .GetByIdsIncludingDeleted(missingTemplateIds)
            .ToDictionary(ct => ct.Id);

        foreach (var task in taskList)
        {
            if (task.Specification != null && task.Specification.CommandTemplate == null && task.Specification.CommandTemplateId > 0)
                if (templates.TryGetValue(task.Specification.CommandTemplateId, out var template))
                    task.Specification.CommandTemplate = template;
        }
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
                .Where(nt => nt.ClusterNodeTypeAggregation != null)
                .GroupBy(nt => nt.ClusterNodeTypeAggregation.Id)
                .Select(g => new ClusterNodeTypeAggregatedReport {
                    ClusterNodeTypeAggregation = g.First().ClusterNodeTypeAggregation,
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
using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;

internal class SubmittedJobInfoRepository : GenericRepository<SubmittedJobInfo>, ISubmittedJobInfoRepository
{
    internal SubmittedJobInfoRepository(MiddlewareContext context)
        : base(context)
    {
    }

    public SubmittedJobInfo GetBySubmittedTaskId(long taskId)
    {
        return _dbSet
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .FirstOrDefault(j => j.Tasks.Any(t => t.Id == taskId));
    }

    public IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId)
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Project)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
            .Where(w => (EF.Property<long>(w, "SubmitterId") == submitterId && w.State < JobState.Finished) ||
                        w.State == JobState.WaitingForServiceAccount)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllUnfinished()
    {
        // Only load what the background state-update loop actually needs:
        //   - Specification.Cluster (SchedulerType, UpdateJobStateByServiceAccount, Name)
        //   - Specification.ClusterUser (credentials fallback)
        //   - Specification.WaitingLimit (IsWaitingLimitExceeded – scalar, loaded with Specification)
        //   - Project (passed to CreateScheduler)
        //   - Submitter (logging + adaptorUserId)
        //   - Active Tasks with Specification.DependsOn (waiting-limit cancel check)
        //   - Tasks.Specification.JobSpecification.{Cluster,ClusterUser,Submitter,ClusterId,ProjectId}
        // NOT loaded (not needed for state update): ClusterProjects, ClusterProjectCredentials,
        //   ResourceConsumed, NodeType, CommandTemplate, SubmitterGroup.
        return _dbSet
            .AsSplitQuery()
            // Active tasks and their task-level specification
            .Include(j => j.Tasks.Where(t => t.State > TaskState.Configuring && t.State < TaskState.Finished))
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.DependsOn)
            .Include(j => j.Tasks.Where(t => t.State > TaskState.Configuring && t.State < TaskState.Finished))
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.JobSpecification)
                        .ThenInclude(js => js.Cluster)
            .Include(j => j.Tasks.Where(t => t.State > TaskState.Configuring && t.State < TaskState.Finished))
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.JobSpecification)
                        .ThenInclude(js => js.ClusterUser)
            .Include(j => j.Tasks.Where(t => t.State > TaskState.Configuring && t.State < TaskState.Finished))
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.JobSpecification)
                        .ThenInclude(js => js.Submitter)
            // Job-level specification (Cluster, ClusterUser, WaitingLimit scalar)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Specification)
                .ThenInclude(s => s.ClusterUser)
            // Top-level navigations used by scheduler factory
            .Include(j => j.Project)
            .Include(j => j.Submitter)
            .Where(j => j.Tasks.Any(we => we.State > TaskState.Configuring && we.State < TaskState.Finished))
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId)
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Tasks)
                .ThenInclude(t => t.NodeType)
            .Where(w => EF.Property<long>(w, "SubmitterId") == submitterId)
            .ToList();
    }
    
    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId)
    {
        return _dbSet
            .AsNoTracking()
            .Where(j => EF.Property<long>(j, "SubmitterId") == submitterId);
    }

    public IQueryable<SubmittedJobInfo> GetJobsQuery()
    {
        return _dbSet
            .AsNoTracking();
    }

    public IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount()
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Project)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Submitter)
            .Where(w => w.State == JobState.WaitingForServiceAccount)
            .OrderBy(w => w.Id)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId, long nodeTypeId)
    {
        var recentThreshold = endTime;

        return _dbSet
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(x => x.Specification.SubProject)
            .Include(x => x.Specification.Submitter)
            .Include(x => x.Tasks)
                .ThenInclude(x => x.ResourceConsumed)
            .Include(x => x.Tasks)
                .ThenInclude(x => x.Specification.CommandTemplate)
            .Where(x => EF.Property<long>(x, "ProjectId") == projectId &&
                        x.StartTime >= startTime &&
                        (x.EndTime == null || x.EndTime <= endTime) &&
                        x.Tasks.Any(y => EF.Property<long>(y, "NodeTypeId") == nodeTypeId) &&
                        (x.State < JobState.Finished || x.EndTime >= recentThreshold))
            .ToList();
    }

    public SubmittedJobInfo GetByIdWithTasks(long id)
    {
        return _dbSet
            .AsSplitQuery()
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
                    .ThenInclude(c => c.ProxyConnection)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
                    .ThenInclude(c => c.ClusterProjects)
                        .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(j => j.Specification)
                .ThenInclude(s => s.ClusterUser)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Project)
                    .ThenInclude(p => p.ClusterProjects)
                        .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(j => j.Project)
                .ThenInclude(p => p.ClusterProjects)
                    .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(j => j.Submitter)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.TaskAllocationNodes)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Project)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.NodeType)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.CommandTemplate)
                        .ThenInclude(ct => ct.TemplateParameters)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.CommandParameterValues)
                        .ThenInclude(cpv => cpv.TemplateParameter)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.DependsOn)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.EnvironmentVariables)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.RequiredNodes)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.TaskParalizationSpecifications)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.ClusterNodeType)
                        .ThenInclude(cnt => cnt.RequestedNodeGroups)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.ClusterNodeType)
                        .ThenInclude(cnt => cnt.ClusterNodeTypeAggregation)
            .FirstOrDefault(j => j.Id == id);
    }

    public SubmittedJobInfo GetByIdForStatus(long id)
    {
        return _dbSet
            .AsNoTrackingWithIdentityResolution()
            .AsSingleQuery()
            .Include(j => j.Submitter)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.TaskAllocationNodes)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.NodeType)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                .ThenInclude(ts => ts.CommandTemplate)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Project) 
            .Include(j => j.Specification)
                .ThenInclude(s => s.SubProject)
            .Include(j => j.Project)
            .FirstOrDefault(j => j.Id == id);
    }

    public IEnumerable<SubmittedJobInfo> GetAllWithoutQueryFilters()
    {
        return _dbSet
            .IgnoreQueryFilters()
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Specification)
            .Include(j => j.Project)
            .ToList();  
    }

    public IQueryable<SubmittedJobInfo> GetQueryableWithoutFilters()
    {
        return _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking();
    }

    public SubmittedJobInfo GetByScheduledJobId(string scheduledJobId)
    {
        return _dbSet
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
            .Include(j => j.Specification)
                .ThenInclude(s => s.ClusterUser)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Project)
            .Include(j => j.Project)
            .Include(j => j.Submitter)
            .FirstOrDefault(j => j.Tasks.Any(t => t.ScheduledJobId == scheduledJobId));
    }

    public SubmittedJobInfo GetByIdWithProject(long id)
    {
        return _dbSet
            .AsNoTracking()
            .Include(j => j.Project)
            .Include(j => j.Submitter)
            .FirstOrDefault(j => j.Id == id);
    }
}
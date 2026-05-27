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
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .FirstOrDefault(j => j.Tasks.Any(t => t.Id == taskId));
    }

    public IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId)
    {
        return _dbSet
            .AsNoTracking()
            .Where(w => (EF.Property<long>(w, "SubmitterId") == submitterId && w.State < JobState.Finished) ||
                        w.State == JobState.WaitingForServiceAccount)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllUnfinished()
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.NodeType)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.ClusterNodeType)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.JobSpecification)
            .ThenInclude(js => js.ClusterUser)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.JobSpecification)
            .ThenInclude(js => js.Cluster)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.JobSpecification)
            .ThenInclude(js => js.Submitter)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.JobSpecification)
            .ThenInclude(js => js.SubmitterGroup)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Specification)
            .ThenInclude(ts => ts.CommandTemplate)
            .Include(j => j.Specification)
            .ThenInclude(s => s.Cluster)
            .Include(j => j.Specification)
            .ThenInclude(s => s.ClusterUser)
            .Include(j => j.Specification)
            .ThenInclude(s => s.Project)
            .Include(j => j.Project)
            .Include(j => j.Submitter)
            .Where(w => w.Tasks.Any(we => we.State > TaskState.Configuring && we.State < TaskState.Finished))
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId)
    {
        return _dbSet
            .AsNoTracking()
            .Where(w => EF.Property<long>(w, "SubmitterId") == submitterId)
            .ToList();
    }
    
    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId)
    {
        return _dbSet
            .Where(j => EF.Property<long>(j, "SubmitterId") == submitterId);
    }

    public IQueryable<SubmittedJobInfo> GetJobsQuery()
    {
        return _dbSet;
    }

    public IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount()
    {
        return _dbSet
            .Where(w => w.State == JobState.WaitingForServiceAccount)
            .OrderBy(w => w.Id)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId, long nodeTypeId)
    {
        return _dbSet
            .AsNoTracking()
            .Include(x => x.Specification.SubProject)
            .Include(x => x.Specification.Submitter)
            .Include(x => x.Tasks)
                .ThenInclude(x => x.ResourceConsumed)
            .Include(x => x.Tasks)
                .ThenInclude(x => x.Specification.CommandTemplate)
            .Where(x => EF.Property<long>(x, "ProjectId") == projectId &&
                        x.StartTime >= startTime &&
                        (x.EndTime == null || x.EndTime <= endTime) &&
                        x.Tasks.Any(y => EF.Property<long>(y, "NodeTypeId") == nodeTypeId))
            .ToList();
    }

    public SubmittedJobInfo GetByIdWithTasks(long id)
    {
        return _dbSet
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Tasks)
                .ThenInclude(t => t.Specification)
                    .ThenInclude(ts => ts.CommandTemplate)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Cluster)
                    .ThenInclude(c => c.ClusterProjects)
            .Include(j => j.Specification)
                .ThenInclude(s => s.ClusterUser)
            .Include(j => j.Specification)
                .ThenInclude(s => s.Project)
            .Include(j => j.Project)
                .ThenInclude(p => p.ClusterProjects)
            .FirstOrDefault(j => j.Id == id);
    }

    public IEnumerable<SubmittedJobInfo> GetAllWithoutQueryFilters()
    {
        return _dbSet
            .IgnoreQueryFilters()
            .Include(j => j.Tasks)
                .ThenInclude(t => t.ResourceConsumed)
            .Include(j => j.Specification)
            .Include(j => j.Project)
            .ToList();  
    }

    public IQueryable<SubmittedJobInfo> GetQueryableWithoutFilters()
    {
        return _dbSet.IgnoreQueryFilters();
    }

    public SubmittedJobInfo GetByScheduledJobId(string scheduledJobId)
    {
        return _dbSet
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
}
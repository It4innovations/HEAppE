using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;

internal class SubmittedTaskInfoRepository : GenericRepository<SubmittedTaskInfo>, ISubmittedTaskInfoRepository
{
    #region Constructors

    internal SubmittedTaskInfoRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public IEnumerable<SubmittedTaskInfo> GetAllUnFinished()
    {
        return _dbSet.Where(w => w.State < TaskState.Finished && w.State > TaskState.Configuring)
            .ToList();
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetAllUnFinishedAsync()
    {
        return await _dbSet.Where(w => w.State < TaskState.Finished && w.State > TaskState.Configuring)
            .ToListAsync();
    }

    public IEnumerable<SubmittedTaskInfo> GetAllFinished()
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Where(w => w.State >= TaskState.Finished)
            .ToList();
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetAllFinishedAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Where(w => w.State >= TaskState.Finished)
            .ToListAsync();
    }

    public IEnumerable<SubmittedTaskInfo> GetFinishedByIds(IEnumerable<long> ids)
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Where(w => w.State >= TaskState.Finished && ids.Contains(w.Id))
            .ToList();
    }

    public async Task<IEnumerable<SubmittedTaskInfo>> GetFinishedByIdsAsync(IEnumerable<long> ids)
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Where(w => w.State >= TaskState.Finished && ids.Contains(w.Id))
            .ToListAsync();
    }

    public SubmittedTaskInfo GetByIdWithJobSpecification(long id)
    {
        var task = _dbSet
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.TaskAllocationNodes)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandTemplate)
                    .ThenInclude(ct => ct.TemplateParameters)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandParameterValues)
                    .ThenInclude(cpv => cpv.TemplateParameter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.DependsOn)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.EnvironmentVariables)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.RequiredNodes)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.TaskParalizationSpecifications)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.RequestedNodeGroups)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.ClusterNodeTypeAggregation)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
                        .ThenInclude(c => c.ProxyConnection)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .FirstOrDefault(t => t.Id == id);

        if (task == null) return null;

        if (task.Specification?.JobSpecification?.Cluster != null)
        {
            _context.Entry(task.Specification.JobSpecification.Cluster)
                .Collection(c => c.ClusterProjects)
                .Query()
                .Include(cp => cp.ClusterProjectCredentials)
                .Load();
        }

        if (task.Project != null)
        {
            _context.Entry(task.Project)
                .Collection(p => p.ClusterProjects)
                .Query()
                .Include(cp => cp.ClusterProjectCredentials)
                .Load();
        }

        return task;
    }

    public async Task<SubmittedTaskInfo> GetByIdWithJobSpecificationAsync(long id)
    {
        var task = await _dbSet
            .Include(t => t.Project)
            .Include(t => t.TaskAllocationNodes)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandTemplate)
                    .ThenInclude(ct => ct.TemplateParameters)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandParameterValues)
                    .ThenInclude(cpv => cpv.TemplateParameter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.DependsOn)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.EnvironmentVariables)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.RequiredNodes)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.TaskParalizationSpecifications)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.RequestedNodeGroups)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.ClusterNodeTypeAggregation)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
                        .ThenInclude(c => c.ProxyConnection)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return null;

        if (task.Specification?.JobSpecification?.Cluster != null)
        {
            await _context.Entry(task.Specification.JobSpecification.Cluster)
                .Collection(c => c.ClusterProjects)
                .Query()
                .Include(cp => cp.ClusterProjectCredentials)
                .LoadAsync();
        }

        if (task.Project != null)
        {
            await _context.Entry(task.Project)
                .Collection(p => p.ClusterProjects)
                .Query()
                .Include(cp => cp.ClusterProjectCredentials)
                .LoadAsync();
        }

        return task;
    }

    public SubmittedTaskInfo GetByIdWithProject(long id)
    {
        return _dbSet
            .Include(t => t.Project)
            .FirstOrDefault(t => t.Id == id);
    }

    public async Task<SubmittedTaskInfo> GetByIdWithProjectAsync(long id)
    {
        return await _dbSet
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public ResourceConsumed GetResourceConsumed(long taskId)
    {
        return _context.Set<ResourceConsumed>().FirstOrDefault(r => r.SubmittedTaskInfoId == taskId);
    }

    public async Task<SubmittedTaskInfo> GetByScheduledJobIdAsync(string scheduledJobId)
    {
        var taskPrefix = $"task:{scheduledJobId}";
        var taskSuffix = $":task:{scheduledJobId}";
        return await _dbSet
            .AsSplitQuery()
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .FirstOrDefaultAsync(t => t.ScheduledJobId == taskPrefix || t.ScheduledJobId == scheduledJobId || t.ScheduledJobId.EndsWith(taskSuffix));
    }

    public async Task<List<SubmittedTaskInfo>> GetTasksByScheduledJobIdAsync(string scheduledJobId)
    {
        var taskPrefix = $"task:{scheduledJobId}";
        var taskSuffix = $":task:{scheduledJobId}";
        return await _dbSet
            .AsSplitQuery()
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Where(t => t.ScheduledJobId == taskPrefix || t.ScheduledJobId == scheduledJobId || t.ScheduledJobId.EndsWith(taskSuffix))
            .ToListAsync();
    }

    public async Task<List<SubmittedTaskInfo>> GetTasksByQSchedulerSessionIdAsync(long sessionId)
    {
        var prefix1 = $"session:{sessionId}";
        var prefix2 = $"session:{sessionId}:";
        return await _dbSet
            .AsSplitQuery()
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Submitter)
            .Where(t => t.ScheduledJobId == prefix1 || t.ScheduledJobId.StartsWith(prefix2))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<TaskState?> GetCurrentTaskStateAsync(long taskId)
    {
        // AsNoTracking ensures we bypass the EF change-tracking cache and always hit the DB.
        // This is needed when a concurrent request (e.g. a callback on a separate UnitOfWork)
        // may have updated the row after this context first loaded the entity.
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => (TaskState?)t.State)
            .FirstOrDefaultAsync();
    }

    #endregion
}
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
            .FirstOrDefault(j => j.Tasks.Any(t => t.Id == taskId));
    }

    public IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId)
    {
        return _dbSet
            .Where(w => (w.Submitter.Id == submitterId && w.State < JobState.Finished) ||
                        w.State == JobState.WaitingForServiceAccount)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllUnfinished()
    {
        return _dbSet
            .Include(j => j.Tasks)
            .Include(j => j.Specification)
            .Include(j => j.Project)
            .Include(j => j.Submitter)
            .Where(w => w.Tasks.Any(we => we.State > TaskState.Configuring && we.State < TaskState.Finished))
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId)
    {
        return _dbSet
            .Where(w => w.Submitter.Id == submitterId)
            .ToList();
    }
    
    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId)
    {
        return _dbSet
            .Where(j => j.Submitter.Id == submitterId);
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
            .Include(x => x.Specification.SubProject)
            .Include(x => x.Specification.Submitter)
            .Include(x => x.Tasks)
                .ThenInclude(x => x.Specification.CommandTemplate)
            .Where(x => x.Project.Id == projectId &&
                        x.StartTime >= startTime &&
                        (x.EndTime == null || x.EndTime <= endTime) &&
                        x.Tasks.Any(y => y.NodeType.Id == nodeTypeId))
            .ToList();
    }

    public SubmittedJobInfo GetByIdWithTasks(long id)
    {
        return _dbSet
            .Include(j => j.Tasks)
            .Include(j => j.Specification)
            .Include(j => j.Project)
            .FirstOrDefault(j => j.Id == id);
    }

    public IEnumerable<SubmittedJobInfo> GetAllWithoutQueryFilters()
    {
        return _dbSet
            .IgnoreQueryFilters()
            .Include(j => j.Tasks)
            .Include(j => j.Specification)
            .Include(j => j.Project)
            .ToList();  
    }

    public IQueryable<SubmittedJobInfo> GetQueryableWithoutFilters()
    {
        return _dbSet.IgnoreQueryFilters();
    }
}
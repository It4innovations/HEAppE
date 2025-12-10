using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;

internal class SubmittedJobInfoRepository : GenericRepository<SubmittedJobInfo>, ISubmittedJobInfoRepository
{
    #region Constructors

    internal SubmittedJobInfoRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId)
    {
        return GetAll().Where(w =>
                (w.Submitter.Id == submitterId && w.State < JobState.Finished) ||
                w.State == JobState.WaitingForServiceAccount)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllUnfinished()
    {
        return GetAll().Where(w => w.Tasks.Any(we => we.State > TaskState.Configuring && we.State < TaskState.Finished))
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId)
    {
        return GetAll().Where(w => w.Submitter.Id == submitterId)
            .ToList();
    }
    
    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId)
    {
        return _dbSet.AsQueryable()
            .Where(j => j.Submitter.Id == submitterId);
    }



    public IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount()
    {
        return GetAll().Where(w => w.State == JobState.WaitingForServiceAccount)
            .OrderBy(w => w.Id)
            .ToList();
    }

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId,
        long nodeTypeId)
    {
        return _dbSet
            .Include(x => x.Specification.SubProject) // Combined Include and ThenInclude
            .Include(x => x.Specification.Submitter) // Combined Include and ThenInclude
            .Include(x => x.Tasks)
            .ThenInclude(x => x.Specification.CommandTemplate) // Combined another Include and ThenInclude
            .Where(x => x.Project.Id == projectId &&
                        x.StartTime >= startTime &&
                        (x.EndTime == null || x.EndTime <= endTime) &&
                        x.Tasks.Any(y => y.NodeType.Id == nodeTypeId))
            .ToList();
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;

public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
{
    public SubmittedJobInfo GetBySubmittedTaskId(long taskId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllUnfinished();
    IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount();

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId,
        long nodeTypeId);

    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId);
    public IQueryable<SubmittedJobInfo> GetJobsQuery();

    public SubmittedJobInfo GetByIdWithTasks(long id);

    /// <summary>
    /// Lightweight query loading only fields needed for status API response.
    /// Does NOT load SSH/scheduler-related navigation properties.
    /// </summary>
    public SubmittedJobInfo GetByIdForStatus(long id);

    public IEnumerable<SubmittedJobInfo> GetAllWithoutQueryFilters();
    IQueryable<SubmittedJobInfo> GetQueryableWithoutFilters();
    SubmittedJobInfo GetByScheduledJobId(string scheduledJobId);
}
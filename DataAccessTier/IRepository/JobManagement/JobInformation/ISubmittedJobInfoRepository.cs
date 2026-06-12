using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;

public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
{
    public SubmittedJobInfo GetBySubmittedTaskId(long taskId);
    Task<SubmittedJobInfo> GetBySubmittedTaskIdAsync(long taskId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId);
    Task<IEnumerable<SubmittedJobInfo>> GetNotFinishedForSubmitterIdAsync(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId);
    Task<IEnumerable<SubmittedJobInfo>> GetAllForSubmitterIdAsync(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllUnfinished();
    Task<IEnumerable<SubmittedJobInfo>> GetAllUnfinishedAsync();
    IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount();
    Task<IEnumerable<SubmittedJobInfo>> GetAllWaitingForServiceAccountAsync();

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId,
        long nodeTypeId);
    Task<IEnumerable<SubmittedJobInfo>> GetJobsForReportAsync(DateTime startTime, DateTime endTime, long projectId,
        long nodeTypeId);

    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId);
    public IQueryable<SubmittedJobInfo> GetJobsQuery();

    public SubmittedJobInfo GetByIdWithTasks(long id);
    Task<SubmittedJobInfo> GetByIdWithTasksAsync(long id);
    public SubmittedJobInfo GetByIdWithProject(long id);
    Task<SubmittedJobInfo> GetByIdWithProjectAsync(long id);

    /// <summary>
    /// Lightweight query loading only fields needed for status API response.
    /// Does NOT load SSH/scheduler-related navigation properties.
    /// </summary>
    public SubmittedJobInfo GetByIdForStatus(long id);
    Task<SubmittedJobInfo> GetByIdForStatusAsync(long id);

    public IEnumerable<SubmittedJobInfo> GetAllWithoutQueryFilters();
    Task<IEnumerable<SubmittedJobInfo>> GetAllWithoutQueryFiltersAsync();
    IQueryable<SubmittedJobInfo> GetQueryableWithoutFilters();
    SubmittedJobInfo GetByScheduledJobId(string scheduledJobId);
    Task<SubmittedJobInfo> GetByScheduledJobIdAsync(string scheduledJobId);
}
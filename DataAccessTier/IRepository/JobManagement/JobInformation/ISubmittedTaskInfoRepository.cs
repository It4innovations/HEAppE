using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;

public interface ISubmittedTaskInfoRepository : IRepository<SubmittedTaskInfo>
{
    IEnumerable<SubmittedTaskInfo> GetAllUnFinished();
    Task<IEnumerable<SubmittedTaskInfo>> GetAllUnFinishedAsync();
    IEnumerable<SubmittedTaskInfo> GetAllFinished();
    Task<IEnumerable<SubmittedTaskInfo>> GetAllFinishedAsync();
    IEnumerable<SubmittedTaskInfo> GetFinishedByIds(IEnumerable<long> ids);
    Task<IEnumerable<SubmittedTaskInfo>> GetFinishedByIdsAsync(IEnumerable<long> ids);
    SubmittedTaskInfo GetByIdWithJobSpecification(long id);
    Task<SubmittedTaskInfo> GetByIdWithJobSpecificationAsync(long id);
    SubmittedTaskInfo GetByIdWithProject(long id);
    Task<SubmittedTaskInfo> GetByIdWithProjectAsync(long id);
    ResourceConsumed GetResourceConsumed(long taskId);
    Task<SubmittedTaskInfo> GetByScheduledJobIdAsync(string scheduledJobId);
    Task<List<SubmittedTaskInfo>> GetTasksByScheduledJobIdAsync(string scheduledJobId);

    /// <summary>
    /// Returns only the current State of a task read directly from the database,
    /// bypassing the EF Core change-tracking cache. Use this when another context
    /// (e.g. a concurrent callback) may have updated the row since this context
    /// first loaded the entity.
    /// </summary>
    Task<TaskState?> GetCurrentTaskStateAsync(long taskId);
}
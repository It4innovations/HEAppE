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
}
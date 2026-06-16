using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IJobSpecificationRepository : IRepository<JobSpecification>
{
    IEnumerable<JobSpecification> GetAllByFileTransferMethod(long fileTransferMethodId);
    Task<IEnumerable<JobSpecification>> GetAllByFileTransferMethodAsync(long fileTransferMethodId);
    JobSpecification GetByIdWithTasksAndSubmitter(long id);
    Task<JobSpecification> GetByIdWithTasksAndSubmitterAsync(long id);
}
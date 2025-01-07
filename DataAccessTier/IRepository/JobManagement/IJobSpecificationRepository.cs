using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IJobSpecificationRepository : IRepository<JobSpecification>
{
    IEnumerable<JobSpecification> GetAllByFileTransferMethod(long fileTransferMethodId);
}
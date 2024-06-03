using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface IJobSpecificationRepository : IRepository<JobSpecification>
    {
        IEnumerable<JobSpecification> GetAllByFileTransferMethod(long fileTransferMethodId);
    }
}
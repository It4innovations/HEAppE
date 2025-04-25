using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class JobSpecificationRepository : GenericRepository<JobSpecification>, IJobSpecificationRepository
{
    #region Constructors

    internal JobSpecificationRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Public methods

    public IEnumerable<JobSpecification> GetAllByFileTransferMethod(long fileTransferMethodId)
    {
        return _dbSet.Where(js => js.FileTransferMethodId == fileTransferMethodId)
            .ToList();
    }

    #endregion
}
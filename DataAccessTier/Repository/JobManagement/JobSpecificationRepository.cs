using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class JobSpecificationRepository : GenericRepository<JobSpecification>, IJobSpecificationRepository
    {
        #region Constructors
        internal JobSpecificationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
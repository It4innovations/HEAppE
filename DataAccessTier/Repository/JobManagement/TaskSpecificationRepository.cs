using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class TaskSpecificationRepository : GenericRepository<TaskSpecification>, ITaskSpecificationRepository
{
    #region Constructors

    internal TaskSpecificationRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}
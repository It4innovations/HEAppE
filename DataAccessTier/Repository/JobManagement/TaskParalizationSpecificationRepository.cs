using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class TaskParalizationSpecificationRepository : GenericRepository<TaskParalizationSpecification>,
    ITaskParalizationSpecificationRepository
{
    #region Constructors

    internal TaskParalizationSpecificationRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}
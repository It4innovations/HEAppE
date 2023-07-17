using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class TaskSpecificationRequiredNodeRepository : GenericRepository<TaskSpecificationRequiredNode>, ITaskSpecificationRequiredNodeRepository
    {
        #region Constructors
        internal TaskSpecificationRequiredNodeRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}

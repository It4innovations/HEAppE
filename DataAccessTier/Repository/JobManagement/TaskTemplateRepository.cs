using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class TaskTemplateRepository : GenericRepository<TaskTemplate>, ITaskTemplateRepository
    {
        #region Constructors
        internal TaskTemplateRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
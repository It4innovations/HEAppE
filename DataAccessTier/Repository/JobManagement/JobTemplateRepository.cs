using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class JobTemplateRepository : GenericRepository<JobTemplate>, IJobTemplateRepository
    {
        #region Constructors
        internal JobTemplateRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
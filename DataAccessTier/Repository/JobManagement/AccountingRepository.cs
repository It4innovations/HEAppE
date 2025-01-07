using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class AccountingRepository : GenericRepository<Accounting>, IAccountingRepository
{
    #region Constructors

    internal AccountingRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}
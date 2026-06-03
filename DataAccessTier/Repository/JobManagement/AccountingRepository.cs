using System.Linq;
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

    public Accounting GetByFormula(string formula)
    {
        return _dbSet.FirstOrDefault(x => x.Formula == formula);
    }
}
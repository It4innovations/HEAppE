using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Accounting> GetByFormulaAsync(string formula)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Formula == formula);
    }
}
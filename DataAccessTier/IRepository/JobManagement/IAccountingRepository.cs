using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IAccountingRepository : IRepository<Accounting>
{
    Accounting GetByFormula(string formula);
    Task<Accounting> GetByFormulaAsync(string formula);
}
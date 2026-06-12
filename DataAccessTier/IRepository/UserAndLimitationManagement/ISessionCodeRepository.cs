using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface ISessionCodeRepository : IRepository<SessionCode>
{
    SessionCode GetByUniqueCode(string uniqueCode);
    Task<SessionCode> GetByUniqueCodeAsync(string uniqueCode);
    SessionCode GetByUser(AdaptorUser user);
    Task<SessionCode> GetByUserAsync(AdaptorUser user);
}
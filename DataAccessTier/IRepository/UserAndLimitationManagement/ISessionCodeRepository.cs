using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface ISessionCodeRepository : IRepository<SessionCode>
{
    SessionCode GetByUniqueCode(string uniqueCode);
    SessionCode GetByUser(AdaptorUser user);
}
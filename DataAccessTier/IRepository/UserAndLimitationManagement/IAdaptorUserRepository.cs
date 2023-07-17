using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserRepository : IRepository<AdaptorUser>
    {
        AdaptorUser GetByName(string username);
    }
}
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserRepository : IRepository<AdaptorUser>
    {
        AdaptorUser GetByName(string username);
    }
}
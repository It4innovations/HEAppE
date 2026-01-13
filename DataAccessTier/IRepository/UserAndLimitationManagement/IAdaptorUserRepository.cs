using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserRepository : IRepository<AdaptorUser>
{
    AdaptorUser GetByName(string username);
    AdaptorUser GetByEmail(string email);
    AdaptorUser GetByNameIgnoreQueryFilters(string username);
    AdaptorUser GetByEmailIgnoreQueryFilters(string email);
    List<AdaptorUser> GetAllUsersInGroup(long groupId);
}
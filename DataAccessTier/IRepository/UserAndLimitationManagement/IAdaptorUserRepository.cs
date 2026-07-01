using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserRepository : IRepository<AdaptorUser>
{
    AdaptorUser GetByName(string username);
    Task<AdaptorUser> GetByNameAsync(string username);
    AdaptorUser GetByApiKey(string apiKey);
    Task<AdaptorUser> GetByApiKeyAsync(string apiKey);
    AdaptorUser GetByEmail(string email);
    Task<AdaptorUser> GetByEmailAsync(string email);
    AdaptorUser GetByNameIgnoreQueryFilters(string username);
    Task<AdaptorUser> GetByNameIgnoreQueryFiltersAsync(string username);
    AdaptorUser GetByEmailIgnoreQueryFilters(string email);
    Task<AdaptorUser> GetByEmailIgnoreQueryFiltersAsync(string email);
    List<AdaptorUser> GetAllUsersInGroup(long groupId);
    Task<List<AdaptorUser>> GetAllUsersInGroupAsync(long groupId);
    IQueryable<AdaptorUser> GetQueryableWithoutFilters();
}
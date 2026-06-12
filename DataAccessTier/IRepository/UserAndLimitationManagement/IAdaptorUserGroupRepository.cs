using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserGroupRepository : IRepository<AdaptorUserGroup>
{
    AdaptorUserGroup GetByIdWithAdaptorUserGroups(long id);
    Task<AdaptorUserGroup> GetByIdWithAdaptorUserGroupsAsync(long id);
    IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndActiveProjects();
    Task<IEnumerable<AdaptorUserGroup>> GetAllWithAdaptorUserGroupsAndActiveProjectsAsync();
    AdaptorUserGroup GetDefaultSubmitterGroup();
    Task<AdaptorUserGroup> GetDefaultSubmitterGroupAsync();
    AdaptorUserGroup GetGroupByUniqueName(string groupName);
    Task<AdaptorUserGroup> GetGroupByUniqueNameAsync(string groupName);
    IEnumerable<AdaptorUserGroup> GetGroupsWithProjects(IEnumerable<long> groupIds);
    Task<IEnumerable<AdaptorUserGroup>> GetGroupsWithProjectsAsync(IEnumerable<long> groupIds);
    IQueryable<AdaptorUserGroup> GetQueryableWithoutFilters();
}
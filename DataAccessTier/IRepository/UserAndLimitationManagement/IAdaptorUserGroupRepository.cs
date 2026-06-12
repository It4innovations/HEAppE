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

    /// <summary>
    /// Lightweight query for LEXIS authentication: loads only groups whose name starts with
    /// <paramref name="namePrefix"/> and are linked to an active project, joining only
    /// the Project table (no clusters, command templates, or users).
    /// </summary>
    Task<List<AdaptorUserGroup>> GetGroupsByPrefixWithActiveProjectsAsync(string namePrefix);
}
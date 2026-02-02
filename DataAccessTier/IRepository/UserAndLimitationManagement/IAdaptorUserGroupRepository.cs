using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IAdaptorUserGroupRepository : IRepository<AdaptorUserGroup>
{
    AdaptorUserGroup GetByIdWithAdaptorUserGroups(long id);
    IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndActiveProjects();
    AdaptorUserGroup GetDefaultSubmitterGroup();
    AdaptorUserGroup GetGroupByUniqueName(string groupName);
    IEnumerable<AdaptorUserGroup> GetGroupsWithProjects(IEnumerable<long> groupIds);
}
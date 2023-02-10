using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserGroupRepository : IRepository<AdaptorUserGroup>
    {
        AdaptorUserGroup GetByIdWithAdaptorUserGroups(long id);
        IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndProject();
        AdaptorUserGroup GetDefaultSubmitterGroup();
        AdaptorUserGroup GetGroupByUniqueName(string groupName);
    }
}
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserGroupRepository : IRepository<AdaptorUserGroup>
    {
        AdaptorUserGroup GetDefaultSubmitterGroup();

        /// <summary>
        /// Retrieve group by unique name.
        /// </summary>
        /// <param name="groupName">Unique group name.</param>
        /// <exception cref="System.InvalidOperationException">is thrown when no group with that name exists or more than one group with this name exist.</exception>
        /// <returns>Single group with that name.</returns>
        AdaptorUserGroup GetGroupByUniqueName(string groupName);
    }
}
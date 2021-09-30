using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IAdaptorUserRoleRepository : IRepository<AdaptorUserRole>
    {
        IEnumerable<AdaptorUserRole> GetAllByUserId(long userId);
        IEnumerable<AdaptorUserRole> GetAllByRoleNames(IEnumerable<string> roleNames);
    }
}
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IOpenStackSessionRepository : IRepository<OpenStackSession>
    {
        OpenStackSession GetByUser(AdaptorUser user);
        IList<OpenStackSession> GetAllActive();
    }
}
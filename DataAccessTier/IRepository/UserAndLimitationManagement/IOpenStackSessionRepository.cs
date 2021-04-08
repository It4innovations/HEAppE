using System.Collections.Generic;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement
{
    public interface IOpenStackSessionRepository : IRepository<OpenStackSession>
    {
        OpenStackSession GetByUser(AdaptorUser user);
        IList<OpenStackSession> GetAllActive();
    }
}
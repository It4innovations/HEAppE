using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IOpenStackSessionRepository : IRepository<OpenStackSession>
{
    OpenStackSession GetByUser(AdaptorUser user);
    Task<OpenStackSession> GetByUserAsync(AdaptorUser user);
    IList<OpenStackSession> GetAllActive();
    Task<IList<OpenStackSession>> GetAllActiveAsync();
}
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IQSchedulerSessionRepository : IRepository<QSchedulerSession>
{
    Task<QSchedulerSession> GetBySessionIdAsync(long sessionId);
    Task<System.Collections.Generic.IEnumerable<QSchedulerSession>> ListSessionsAsync(long userId, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null);
}

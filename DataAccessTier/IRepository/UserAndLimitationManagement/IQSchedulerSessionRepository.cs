using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

public interface IQSchedulerSessionRepository : IRepository<QSchedulerSession>
{
    /// <summary>
    ///     Finds a session by its QScheduler ID. Session IDs are unique only per QScheduler instance,
    ///     so pass <paramref name="clusterId"/> whenever it is known.
    /// </summary>
    Task<QSchedulerSession> GetBySessionIdAsync(long sessionId, long? clusterId = null);

    /// <summary>
    ///     All sessions with this QScheduler ID, across clusters (used when the cluster is not known yet, e.g. callbacks).
    /// </summary>
    Task<IEnumerable<QSchedulerSession>> GetAllBySessionIdAsync(long sessionId);

    Task<IEnumerable<QSchedulerSession>> ListSessionsAsync(long userId, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null);

    /// <summary>
    ///     Sessions whose consumption may still change: not closed yet, or closed without a final value from QScheduler.
    /// </summary>
    Task<IEnumerable<QSchedulerSession>> GetSessionsWithPendingConsumptionAsync();

    /// <summary>
    ///     Sessions of the given projects that were opened (or created, if never opened) within the time window.
    /// </summary>
    IList<QSchedulerSession> GetSessionsForReport(IEnumerable<long> projectIds, System.DateTime start, System.DateTime end, long? clusterId = null, long? userId = null);
}

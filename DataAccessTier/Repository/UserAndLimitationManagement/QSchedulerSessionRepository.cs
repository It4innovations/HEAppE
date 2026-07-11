using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class QSchedulerSessionRepository : GenericRepository<QSchedulerSession>, IQSchedulerSessionRepository
{
    public QSchedulerSessionRepository(MiddlewareContext context)
        : base(context)
    {
    }

    public async Task<QSchedulerSession> GetBySessionIdAsync(long sessionId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    public async Task<System.Collections.Generic.IEnumerable<QSchedulerSession>> ListSessionsAsync(long userId, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null)
    {
        var query = _dbSet.Where(s => s.UserId == userId);

        if (state.HasValue)
        {
            query = query.Where(s => s.State == state.Value);
        }

        if (clusterId.HasValue)
        {
            query = query.Where(s => s.ClusterId == clusterId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == projectId.Value);
        }

        return await query.ToListAsync();
    }
}

using System;
using System.Collections.Generic;
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

    public async Task<QSchedulerSession> GetBySessionIdAsync(long sessionId, long? clusterId = null)
    {
        var query = _dbSet.Where(s => s.SessionId == sessionId);
        if (clusterId.HasValue)
        {
            query = query.Where(s => s.ClusterId == clusterId.Value);
        }

        return await query
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<QSchedulerSession>> GetAllBySessionIdAsync(long sessionId)
    {
        return await _dbSet
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<QSchedulerSession>> ListSessionsAsync(long userId, QSchedulerSessionState? state = null, long? clusterId = null, long? projectId = null)
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

    public async Task<IEnumerable<QSchedulerSession>> GetSessionsWithPendingConsumptionAsync()
    {
        return await _dbSet
            .Where(s => !s.IsConsumptionFinal)
            .ToListAsync();
    }

    public IList<QSchedulerSession> GetSessionsForReport(IEnumerable<long> projectIds, DateTime start, DateTime end, long? clusterId = null, long? userId = null)
    {
        var ids = projectIds?.ToList() ?? new List<long>();
        if (ids.Count == 0)
        {
            return new List<QSchedulerSession>();
        }

        var query = _dbSet
            .AsNoTracking()
            .Where(s => ids.Contains(s.ProjectId))
            .Where(s => (s.OpenedAt ?? s.CreatedAt) >= start && (s.OpenedAt ?? s.CreatedAt) <= end);

        if (clusterId.HasValue)
        {
            query = query.Where(s => s.ClusterId == clusterId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(s => s.UserId == userId.Value);
        }

        return query.ToList();
    }
}

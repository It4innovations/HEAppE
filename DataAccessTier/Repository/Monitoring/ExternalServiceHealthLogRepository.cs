using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.Monitoring;
using HEAppE.DomainObjects.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.Monitoring;

internal class ExternalServiceHealthLogRepository : GenericRepository<ExternalServiceHealthLog>, IExternalServiceHealthLogRepository
{
    internal ExternalServiceHealthLogRepository(MiddlewareContext context)
        : base(context)
    {
    }

    public async Task<List<ExternalServiceHealthLog>> GetLogsInTimeRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.Timestamp >= from && x.Timestamp <= to)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task<List<ExternalServiceHealthLog>> GetFilteredLogsInTimeRangeAsync(DateTime from, DateTime to, string serviceName = null, long? clusterId = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(x => x.Timestamp >= from && x.Timestamp <= to);

        if (!string.IsNullOrEmpty(serviceName))
        {
            query = query.Where(x => x.ServiceName == serviceName);
        }

        if (clusterId.HasValue)
        {
            query = query.Where(x => x.ClusterId == clusterId.Value);
        }

        return await query.OrderBy(x => x.Timestamp).ToListAsync();
    }

    public async Task<List<ExternalServiceHealthLog>> GetLogsByJobIdAsync(long jobId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderBy(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task BulkInsertAsync(IEnumerable<ExternalServiceHealthLog> logs)
    {
        if (logs == null) return;
        await _dbSet.AddRangeAsync(logs);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOlderThanAsync(DateTime threshold)
    {
        await _dbSet
            .Where(x => x.Timestamp < threshold)
            .ExecuteDeleteAsync();
    }
}

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
            .ToListAsync();
    }

    public async Task DeleteOlderThanAsync(DateTime threshold)
    {
        var logsToDelete = await _dbSet
            .Where(x => x.Timestamp < threshold)
            .ToListAsync();

        if (logsToDelete.Any())
        {
            _dbSet.RemoveRange(logsToDelete);
        }
    }
}

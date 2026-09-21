using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;

namespace HEAppE.DataAccessTier.IRepository.Monitoring;

public interface IExternalServiceHealthLogRepository : IRepository<ExternalServiceHealthLog>
{
    Task<List<ExternalServiceHealthLog>> GetLogsInTimeRangeAsync(DateTime from, DateTime to);
    Task<List<ExternalServiceHealthLog>> GetFilteredLogsInTimeRangeAsync(DateTime from, DateTime to, string serviceName = null, long? clusterId = null);
    Task<List<ExternalServiceHealthLog>> GetLogsByJobIdAsync(long jobId);
    Task BulkInsertAsync(IEnumerable<ExternalServiceHealthLog> logs);
    Task DeleteOlderThanAsync(DateTime threshold);
}

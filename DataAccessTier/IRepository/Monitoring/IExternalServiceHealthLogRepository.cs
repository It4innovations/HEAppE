using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;

namespace HEAppE.DataAccessTier.IRepository.Monitoring;

public interface IExternalServiceHealthLogRepository : IRepository<ExternalServiceHealthLog>
{
    Task<List<ExternalServiceHealthLog>> GetLogsInTimeRangeAsync(DateTime from, DateTime to);
    Task DeleteOlderThanAsync(DateTime threshold);
}

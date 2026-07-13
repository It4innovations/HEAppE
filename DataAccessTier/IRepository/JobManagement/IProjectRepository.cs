using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IProjectRepository : IRepository<Project>
{
    IEnumerable<Project> GetAllActiveProjects();
    Task<IEnumerable<Project>> GetAllActiveProjectsAsync();
    Project GetByAccountingString(string accountingString);
    Task<Project> GetByAccountingStringAsync(string accountingString);
    
    Project GetByIdWithClusterProjects(long projectId);
    Task<Project> GetByIdWithClusterProjectsAsync(long projectId);
    Project GetByAccountingStringWithClusterProjects(string accountingString);
    Task<Project> GetByAccountingStringWithClusterProjectsAsync(string accountingString);
    IEnumerable<Project> GetAllWithClusterProjects();
    Task<IEnumerable<Project>> GetAllWithClusterProjectsAsync();
    Project GetByIdWithSubProjects(long id);
    Task<Project> GetByIdWithSubProjectsAsync(long id);
    Task<Project> GetByIdWithAggregationsAsync(long id);
}
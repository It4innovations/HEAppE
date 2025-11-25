using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IProjectRepository : IRepository<Project>
{
    IEnumerable<Project> GetAllActiveProjects();
    Project GetByAccountingString(string accountingString);
    
    Project GetByIdWithClusterProjects(long projectId);
    
    
}
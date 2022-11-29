using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface IProjectRepository : IRepository<Project>
    {
        IEnumerable<Project> GetAllActiveProjects();
        Project GetByAccountingString(string accountingString);
    }
}
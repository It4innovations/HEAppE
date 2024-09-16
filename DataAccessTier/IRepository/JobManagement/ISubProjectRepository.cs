using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface ISubProjectRepository : IRepository<SubProject>
    {
        SubProject GetByIdentifier(string accountingString, long projectId);
    }
}
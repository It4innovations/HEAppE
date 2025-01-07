using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface ISubProjectRepository : IRepository<SubProject>
{
    SubProject GetByIdentifier(string accountingString, long projectId);
}
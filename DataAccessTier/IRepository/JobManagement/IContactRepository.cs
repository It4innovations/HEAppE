using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IContactRepository : IRepository<Contact>
{
    Contact GetByEmail(string email);
}
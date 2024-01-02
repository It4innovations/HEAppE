using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface IContactRepository : IRepository<Contact>
    {
        Contact GetByEmail(string email);
    }
}
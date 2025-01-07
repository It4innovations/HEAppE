using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class ContactRepository : GenericRepository<Contact>, IContactRepository
{
    #region Constructors

    internal ContactRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public Contact GetByEmail(string email)
    {
        return _context.Contacts.FirstOrDefault(p => p.Email == email);
    }

    #endregion
}
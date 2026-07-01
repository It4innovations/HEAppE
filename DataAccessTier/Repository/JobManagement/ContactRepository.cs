using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Contact> GetByEmailAsync(string email)
    {
        return await _context.Contacts.FirstOrDefaultAsync(p => p.Email == email);
    }

    #endregion
}
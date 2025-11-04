using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class AdaptorUserRepository : GenericRepository<AdaptorUser>, IAdaptorUserRepository
{
    #region Constructors

    internal AdaptorUserRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public AdaptorUser GetByName(string username)
    {
        return GetAll().Where(w => w.Username == username)
            .FirstOrDefault();
    }
    
    public AdaptorUser GetByNameIgnoreQueryFilters(string username)
    {
        return _dbSet
            .Include(x=>x.AdaptorUserUserGroupRoles)
            .IgnoreQueryFilters() 
            .FirstOrDefault(w => w.Username == username);
    }
    
    public AdaptorUser GetByEmail(string email)
    {
        return GetAll().Where(w => w.Email == email)
            .FirstOrDefault();
    }
    
    


    #endregion
}
using System.Threading.Tasks;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class SessionCodeRepository : GenericRepository<SessionCode>, ISessionCodeRepository
{
    #region Constructors

    internal SessionCodeRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public SessionCode GetByUniqueCode(string uniqueCode)
    {
        return _dbSet
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole) 
            
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
        
            .SingleOrDefault(w => w.UniqueCode == uniqueCode);
    }

    public async Task<SessionCode> GetByUniqueCodeAsync(string uniqueCode)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole) 
            
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
        
            .SingleOrDefaultAsync(w => w.UniqueCode == uniqueCode);
    }

    public SessionCode GetByUser(AdaptorUser user)
    {
        return _dbSet
            .OfType<SessionCode>()
            .OrderByDescending(w=> w.Id)
            .FirstOrDefault(w => w.User.Id == user.Id);
    }

    public async Task<SessionCode> GetByUserAsync(AdaptorUser user)
    {
        return await _dbSet
            .OfType<SessionCode>()
            .OrderByDescending(w=> w.Id)
            .FirstOrDefaultAsync(w => w.User.Id == user.Id);
    }



    #endregion
}
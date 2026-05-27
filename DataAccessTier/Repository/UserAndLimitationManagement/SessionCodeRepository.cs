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
            //.AsSplitQuery()
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole) 
            
            .Include(s => s.User)
            .ThenInclude(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
        
            .SingleOrDefault(w => w.UniqueCode == uniqueCode);
    }

    public SessionCode GetByUser(AdaptorUser user)
    {
        return _dbSet
            .OfType<SessionCode>()
            .OrderByDescending(w=> w.Id)
            .FirstOrDefault(w => w.User.Id == user.Id);
    }



    #endregion
}
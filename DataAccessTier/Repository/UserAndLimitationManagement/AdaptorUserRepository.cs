using System.Collections.Generic;
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
        return _dbSet
            // 1. Větev: Načtení rolí (bez ContainedRoleTypes, ty se načtou automaticky s rolí)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
        
            // 2. Větev: Načtení skupin a projektů
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
        
            .FirstOrDefault(w => w.Username == username);
    }

    public AdaptorUser GetByApiKey(string apiKey)
    {
        return _dbSet
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .SingleOrDefault(u => u.Password == apiKey);
    }


    public override AdaptorUser GetById(long id)
    {
        return _dbSet
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .SingleOrDefault(u => u.Id == id);
    }
    
    public AdaptorUser GetByNameIgnoreQueryFilters(string username)
    {
        return _dbSet
            .Include(x=>x.AdaptorUserUserGroupRoles)
            .IgnoreQueryFilters() 
            .FirstOrDefault(w => w.Username == username);
    }
    
    public AdaptorUser GetByEmailIgnoreQueryFilters(string email)
    {
        return _dbSet
            .Include(x=>x.AdaptorUserUserGroupRoles)
            .IgnoreQueryFilters() 
            .FirstOrDefault(w => w.Email == email);
    }

    public List<AdaptorUser> GetAllUsersInGroup(long groupId)
    {
        return _dbSet
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .Where(u => u.AdaptorUserUserGroupRoles
                .Any(ugr => ugr.AdaptorUserGroup.Id == groupId))
            .ToList();
    }

    public AdaptorUser GetByEmail(string email)
    {
        return GetAll().Where(w => w.Email == email)
            .FirstOrDefault();
    }
    
    


    #endregion
}
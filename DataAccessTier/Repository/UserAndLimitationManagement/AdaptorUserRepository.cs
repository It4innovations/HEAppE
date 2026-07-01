using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            .AsSplitQuery()
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)

            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
        
            .FirstOrDefault(w => w.Username == username);
    }

    public async Task<AdaptorUser> GetByNameAsync(string username)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)

            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
        
            .FirstOrDefaultAsync(w => w.Username == username);
    }

    public AdaptorUser GetByApiKey(string apiKey)
    {
        return _dbSet
            .AsSplitQuery()
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .SingleOrDefault(u => u.Password == apiKey);
    }

    public async Task<AdaptorUser> GetByApiKeyAsync(string apiKey)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .SingleOrDefaultAsync(u => u.Password == apiKey);
    }

    public override AdaptorUser GetById(long id)
    {
        return _dbSet
            .AsSplitQuery()
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .ThenInclude(ug => ug.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .SingleOrDefault(u => u.Id == id);
    }
    
    public AdaptorUser GetByNameIgnoreQueryFilters(string username)
    {
        return _dbSet
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .IgnoreQueryFilters() 
            .FirstOrDefault(w => w.Username == username);
    }

    public async Task<AdaptorUser> GetByNameIgnoreQueryFiltersAsync(string username)
    {
        return await _dbSet
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .IgnoreQueryFilters() 
            .FirstOrDefaultAsync(w => w.Username == username);
    }
    
    public AdaptorUser GetByEmailIgnoreQueryFilters(string email)
    {
        return _dbSet
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .IgnoreQueryFilters() 
            .FirstOrDefault(w => w.Email == email);
    }

    public async Task<AdaptorUser> GetByEmailIgnoreQueryFiltersAsync(string email)
    {
        return await _dbSet
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Include(x => x.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .IgnoreQueryFilters() 
            .FirstOrDefaultAsync(w => w.Email == email);
    }

    public List<AdaptorUser> GetAllUsersInGroup(long groupId)
    {
        return _dbSet
            .Include(u => u.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserGroup)
                    .ThenInclude(g => g.Project)
            .Include(u => u.AdaptorUserUserGroupRoles)
                .ThenInclude(ugr => ugr.AdaptorUserRole)
            .Where(u => u.AdaptorUserUserGroupRoles
                .Any(ugr => ugr.AdaptorUserGroupId == groupId))
            .ToList();
    }

    public async Task<List<AdaptorUser>> GetAllUsersInGroupAsync(long groupId)
    {
        return await _dbSet
            .Include(u => u.AdaptorUserUserGroupRoles)
            .ThenInclude(ugr => ugr.AdaptorUserGroup)
            .Where(u => u.AdaptorUserUserGroupRoles
                .Any(ugr => ugr.AdaptorUserGroup.Id == groupId))
            .ToListAsync();
    }

    public IQueryable<AdaptorUser> GetQueryableWithoutFilters()
    {
        return _dbSet
            .IgnoreQueryFilters();
    }

    public AdaptorUser GetByEmail(string email)
    {
        return _dbSet.FirstOrDefault(w => w.Email == email);
    }

    public async Task<AdaptorUser> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(w => w.Email == email);
    }

    #endregion
}
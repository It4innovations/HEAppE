using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class AdaptorUserGroupRepository : GenericRepository<AdaptorUserGroup>, IAdaptorUserGroupRepository
{
    #region Instances

    private readonly string _defaultGroupName = "default";

    #endregion

    #region Constructors

    internal AdaptorUserGroupRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public AdaptorUserGroup GetByIdWithAdaptorUserGroups(long id)
    {
        return _dbSet.Where(w => w.Id == id)
            .Include(i => i.AdaptorUserUserGroupRoles)
            .ThenInclude(i => i.AdaptorUser)
            .FirstOrDefault();
    }

    public async Task<AdaptorUserGroup> GetByIdWithAdaptorUserGroupsAsync(long id)
    {
        return await _dbSet.Where(w => w.Id == id)
            .Include(i => i.AdaptorUserUserGroupRoles)
            .ThenInclude(i => i.AdaptorUser)
            .FirstOrDefaultAsync();
    }

    public IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndActiveProjects()
    {
        return _dbSet.Include(p => p.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Include(p => p.Project)
            .ThenInclude(i => i.CommandTemplates)
            .ThenInclude(i => i.TemplateParameters)
            .Include(i => i.AdaptorUserUserGroupRoles)
            .ThenInclude(i => i.AdaptorUser)
            .Where(p => p.Project.EndDate >= DateTime.UtcNow)
            .ToList();
    }

    public async Task<IEnumerable<AdaptorUserGroup>> GetAllWithAdaptorUserGroupsAndActiveProjectsAsync()
    {
        return await _dbSet.Include(p => p.Project)
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Include(p => p.Project)
            .ThenInclude(i => i.CommandTemplates)
            .ThenInclude(i => i.TemplateParameters)
            .Include(i => i.AdaptorUserUserGroupRoles)
            .ThenInclude(i => i.AdaptorUser)
            .Where(p => p.Project.EndDate >= DateTime.UtcNow)
            .ToListAsync();
    }

    public AdaptorUserGroup GetDefaultSubmitterGroup()
    {
        return _dbSet.FirstOrDefault(w => w.Name == _defaultGroupName);
    }

    public async Task<AdaptorUserGroup> GetDefaultSubmitterGroupAsync()
    {
        return await _dbSet.FirstOrDefaultAsync(w => w.Name == _defaultGroupName);
    }

    public AdaptorUserGroup GetGroupByUniqueName(string groupName)
    {
        return _dbSet.SingleOrDefault(g => g.Name == groupName);
    }

    public async Task<AdaptorUserGroup> GetGroupByUniqueNameAsync(string groupName)
    {
        return await _dbSet.SingleOrDefaultAsync(g => g.Name == groupName);
    }

    public IEnumerable<AdaptorUserGroup> GetGroupsWithProjects(IEnumerable<long> groupIds)
    {
        return _dbSet
            .Include(i => i.Project) 
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Where(g => groupIds.Contains(g.Id)) 
            .ToList();
    }

    public async Task<IEnumerable<AdaptorUserGroup>> GetGroupsWithProjectsAsync(IEnumerable<long> groupIds)
    {
        return await _dbSet
            .Include(i => i.Project) 
            .ThenInclude(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Where(g => groupIds.Contains(g.Id)) 
            .ToListAsync();
    }

    public IQueryable<AdaptorUserGroup> GetQueryableWithoutFilters()
    {
        return _dbSet
            .IgnoreQueryFilters();
    }

    public async Task<List<AdaptorUserGroup>> GetGroupsByPrefixWithActiveProjectsAsync(string namePrefix)
    {
        return await _dbSet
            .Include(g => g.Project)
            .Where(g => g.Name.StartsWith(namePrefix) && g.Project.EndDate >= DateTime.UtcNow)
            .ToListAsync();
    }

    #endregion
}
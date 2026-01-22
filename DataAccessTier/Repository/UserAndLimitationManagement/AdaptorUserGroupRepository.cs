using System;
using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<AdaptorUserGroup> GetAllWithAdaptorUserGroupsAndActiveProjects()
    {
        return _dbSet.Include(p => p.Project)
            .ThenInclude(i => i.CommandTemplates)
            .ThenInclude(i => i.TemplateParameters)
            .Include(i => i.AdaptorUserUserGroupRoles)
            .ThenInclude(i => i.AdaptorUser)
            .Where(p => p.Project.EndDate >= DateTime.UtcNow)
            .ToList();
    }

    public AdaptorUserGroup GetDefaultSubmitterGroup()
    {
        return GetAll().FirstOrDefault(w => w.Name == _defaultGroupName);
    }

    public AdaptorUserGroup GetGroupByUniqueName(string groupName)
    {
        return GetAll().SingleOrDefault(g => g.Name == groupName);
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

    #endregion
}
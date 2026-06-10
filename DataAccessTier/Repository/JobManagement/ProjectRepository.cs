using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    #region Constructors

    internal ProjectRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public IEnumerable<Project> GetAllActiveProjects()
    {
        return _dbSet.Where(p => p.EndDate >= DateTime.UtcNow)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.ProjectContacts)
                .ThenInclude(x => x.Contact)
            .Include(x => x.ClusterProjects)
                .ThenInclude(x => x.Cluster)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .ToList();
    }

    public Project GetByAccountingString(string accountingString)
    {
        return _context.Projects
            .AsNoTracking()
            .FirstOrDefault(p => p.AccountingString == accountingString);
    }

    public Project GetByAccountingStringWithClusterProjects(string accountingString)
    {
        return _context.Projects
            .AsNoTracking()
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .FirstOrDefault(p => p.AccountingString == accountingString);
    }

    public Project GetByIdWithClusterProjects(long projectId)
    {
        return _context.Projects
            .AsNoTracking()
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefault(p => p.Id == projectId);
    }

    public IEnumerable<Project> GetAllWithClusterProjects()
    {
        return _context.Projects
            .AsNoTracking()
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .ToList();
    }

    public Project GetByIdWithSubProjects(long id)
    {
        return _dbSet
            .AsNoTracking()
            .Include(p => p.SubProjects)
            .FirstOrDefault(p => p.Id == id);
    }

    public override Project GetById(long id)
    {
        return _dbSet
            .Include(x => x.ProjectContacts)
                .ThenInclude(x => x.Contact)
            .Include(x => x.ClusterProjects)
                .ThenInclude(x => x.Cluster)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .Include(x => x.AdaptorUserGroups)
            .FirstOrDefault(p => p.Id == id);
    }

    #endregion
}
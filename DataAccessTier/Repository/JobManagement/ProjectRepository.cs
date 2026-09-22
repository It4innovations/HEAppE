using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            .Include(x => x.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .ToList();
    }

    public async Task<IEnumerable<Project>> GetAllActiveProjectsAsync()
    {
        return await _dbSet.Where(p => p.EndDate >= DateTime.UtcNow)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.ProjectContacts)
                .ThenInclude(x => x.Contact)
            .Include(x => x.ClusterProjects)
                .ThenInclude(x => x.Cluster)
            .Include(x => x.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .ToListAsync();
    }

    public Project GetByAccountingString(string accountingString)
    {
        return _context.Projects
            .AsNoTracking()
            .FirstOrDefault(p => p.AccountingString == accountingString);
    }

    public async Task<Project> GetByAccountingStringAsync(string accountingString)
    {
        return await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountingString == accountingString);
    }

    public Project GetByAccountingStringWithClusterProjects(string accountingString)
    {
        return _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefault(p => p.AccountingString == accountingString);
    }

    public async Task<Project> GetByAccountingStringWithClusterProjectsAsync(string accountingString)
    {
        return await _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefaultAsync(p => p.AccountingString == accountingString);
    }

    public Project GetByIdWithClusterProjects(long projectId)
    {
        return _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefault(p => p.Id == projectId);
    }

    public async Task<Project> GetByIdWithClusterProjectsAsync(long projectId)
    {
        return await _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
            .ThenInclude(cp => cp.ClusterProjectCredentials)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public IEnumerable<Project> GetAllWithClusterProjects()
    {
        return _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .ToList();
    }

    public async Task<IEnumerable<Project>> GetAllWithClusterProjectsAsync()
    {
        return await _context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .ToListAsync();
    }

    public Project GetByIdWithSubProjects(long id)
    {
        return _dbSet
            .AsNoTracking()
            .Include(p => p.SubProjects)
            .FirstOrDefault(p => p.Id == id);
    }

    public async Task<Project> GetByIdWithSubProjectsAsync(long id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.SubProjects)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public Project GetByIdWithAccountingStates(long id)
    {
        return _dbSet
            .Include(p => p.AccountingStates)
            .FirstOrDefault(p => p.Id == id);
    }

    public async Task<Project> GetByIdWithAccountingStatesAsync(long id)
    {
        return await _dbSet
            .Include(p => p.AccountingStates)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public override Project GetById(long id)
    {
        return _dbSet
            .AsSplitQuery()
            .Include(x => x.ProjectContacts)
                .ThenInclude(x => x.Contact)
            .Include(x => x.ClusterProjects)
                .ThenInclude(x => x.Cluster)
            .Include(x => x.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .Include(x => x.AdaptorUserGroups)
            .FirstOrDefault(p => p.Id == id);
    }

    public override async Task<Project> GetByIdAsync(long id)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(x => x.ProjectContacts)
                .ThenInclude(x => x.Contact)
            .Include(x => x.ClusterProjects)
                .ThenInclude(x => x.Cluster)
            .Include(x => x.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(x => x.CommandTemplates)
                .ThenInclude(ct => ct.TemplateParameters)
            .Include(x => x.AdaptorUserGroups)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> GetByIdWithAggregationsAsync(long id)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.Cluster)
            .Include(p => p.ClusterProjects)
                .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Include(p => p.ProjectClusterNodeTypeAggregations)
                .ThenInclude(pcna => pcna.ClusterNodeTypeAggregation)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    #endregion
}
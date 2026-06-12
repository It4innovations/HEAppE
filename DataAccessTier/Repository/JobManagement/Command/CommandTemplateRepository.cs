using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command;

internal class CommandTemplateRepository : GenericRepository<CommandTemplate>, ICommandTemplateRepository
{
    #region Constructors

    internal CommandTemplateRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    public IList<CommandTemplate> GetCommandTemplatesByProjectId(long projectId)
    {
        return _dbSet.Where(w => w.ProjectId == projectId || w.ProjectId == null)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Project)
            .Include(i => i.ClusterNodeType)
            .Include(i => i.TemplateParameters)
            .ToList();
    }

    public async Task<IList<CommandTemplate>> GetCommandTemplatesByProjectIdAsync(long projectId)
    {
        return await _dbSet.Where(w => w.ProjectId == projectId || w.ProjectId == null)
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Project)
            .Include(i => i.ClusterNodeType)
            .Include(i => i.TemplateParameters)
            .ToListAsync();
    }

    /// <inheritdoc />
    public IList<CommandTemplate> GetByIdsIncludingDeleted(IEnumerable<long> ids)
    {
        var idList = ids?.ToList();
        if (idList == null || !idList.Any()) return new List<CommandTemplate>();
        return _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ct => idList.Contains(ct.Id))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IList<CommandTemplate>> GetByIdsIncludingDeletedAsync(IEnumerable<long> ids)
    {
        var idList = ids?.ToList();
        if (idList == null || !idList.Any()) return new List<CommandTemplate>();
        return await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ct => idList.Contains(ct.Id))
            .ToListAsync();
    }
}
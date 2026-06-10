using System.Collections.Generic;
using System.Linq;
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

    public IList<CommandTemplate> GetCommandTemplatesByProjectIds(IEnumerable<long> projectIds)
    {
        var projectIdList = projectIds?.ToList() ?? new List<long>();
        return _dbSet.Where(w => w.ProjectId == null || (w.ProjectId.HasValue && projectIdList.Contains(w.ProjectId.Value)))
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Project)
            .Include(i => i.ClusterNodeType)
            .Include(i => i.TemplateParameters)
            .ToList();
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
}
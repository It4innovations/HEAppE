using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation;

internal class ClusterNodeTypeRepository : GenericRepository<ClusterNodeType>, IClusterNodeTypeRepository
{
    #region Constructors

    internal ClusterNodeTypeRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Public methods

    public IEnumerable<ClusterNodeType> GetAllWithPossibleCommands()
    {
        return _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Cluster)
                .ThenInclude(c => c.ClusterProjects)
                    .ThenInclude(cp => cp.Project)
            .Include(i => i.ClusterNodeTypeAggregation)
                .ThenInclude(a => a.ClusterNodeTypeAggregationAccountings)
                    .ThenInclude(acc => acc.Accounting)
            .Include(i => i.PossibleCommands)
                .ThenInclude(i => i.TemplateParameters)
            .ToList();
    }

    public async Task<IEnumerable<ClusterNodeType>> GetAllWithPossibleCommandsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Cluster)
                .ThenInclude(c => c.ClusterProjects)
                    .ThenInclude(cp => cp.Project)
            .Include(i => i.ClusterNodeTypeAggregation)
                .ThenInclude(a => a.ClusterNodeTypeAggregationAccountings)
                    .ThenInclude(acc => acc.Accounting)
            .Include(i => i.PossibleCommands)
                .ThenInclude(i => i.TemplateParameters)
            .ToListAsync();
    }

    public IEnumerable<ClusterNodeType> GetAllByFileTransferMethod(long fileTransferMethodId)
    {
        return _dbSet.Where(nt => nt.FileTransferMethodId == fileTransferMethodId)
            .ToList();
    }

    public async Task<IEnumerable<ClusterNodeType>> GetAllByFileTransferMethodAsync(long fileTransferMethodId)
    {
        return await _dbSet.Where(nt => nt.FileTransferMethodId == fileTransferMethodId)
            .ToListAsync();
    }

    public ClusterNodeType GetByIdWithClusterAndProjects(long id)
    {
        return _dbSet
            .Include(i => i.Cluster)
                .ThenInclude(c => c.ClusterProjects)
                    .ThenInclude(cp => cp.Project)
            .Include(i => i.ClusterNodeTypeAggregation)
                .ThenInclude(a => a.ClusterNodeTypeAggregationAccountings)
                    .ThenInclude(acc => acc.Accounting)
            .Include(i => i.PossibleCommands)
                .ThenInclude(pc => pc.TemplateParameters)
            .FirstOrDefault(i => i.Id == id);
    }

    public async Task<ClusterNodeType> GetByIdWithClusterAndProjectsAsync(long id)
    {
        return await _dbSet
            .Include(i => i.Cluster)
                .ThenInclude(c => c.ClusterProjects)
                    .ThenInclude(cp => cp.Project)
            .Include(i => i.ClusterNodeTypeAggregation)
                .ThenInclude(a => a.ClusterNodeTypeAggregationAccountings)
                    .ThenInclude(acc => acc.Accounting)
            .Include(i => i.PossibleCommands)
                .ThenInclude(pc => pc.TemplateParameters)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    #endregion
}
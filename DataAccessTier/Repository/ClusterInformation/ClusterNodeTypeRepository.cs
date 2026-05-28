using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<ClusterNodeType> GetAllByFileTransferMethod(long fileTransferMethodId)
    {
        return _dbSet.Where(nt => nt.FileTransferMethodId == fileTransferMethodId)
            .ToList();
    }

    #endregion
}
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class ClusterNodeTypeAggregationAccountingRepository : IClusterNodeTypeAggregationAccountingRepository
{
    #region Constructors

    internal ClusterNodeTypeAggregationAccountingRepository(MiddlewareContext context)
    {
        _context = context;
        _dbSet = context.Set<ClusterNodeTypeAggregationAccounting>();
    }

    #endregion

    public IList<ClusterNodeTypeAggregationAccounting> GetAll()
    {
        return _dbSet.ToList();
    }

    public ClusterNodeTypeAggregationAccounting GetById(long clusterNodeTypeAggregationId, long accountingId)
    {
        return _dbSet.Find(clusterNodeTypeAggregationId, accountingId);
    }

    public ClusterNodeTypeAggregationAccounting GetByIdIncludeSoftDeleted(long clusterNodeTypeAggregationId,
        long accountingId)
    {
        return _dbSet.IgnoreQueryFilters().FirstOrDefault(c =>
            c.ClusterNodeTypeAggregationId == clusterNodeTypeAggregationId && c.AccountingId == accountingId);
    }

    public void Insert(ClusterNodeTypeAggregationAccounting entity)
    {
        _dbSet.Add(entity);
    }

    public virtual void Update(ClusterNodeTypeAggregationAccounting entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    #region Instances

    protected readonly MiddlewareContext _context;
    protected readonly DbSet<ClusterNodeTypeAggregationAccounting> _dbSet;

    #endregion
}
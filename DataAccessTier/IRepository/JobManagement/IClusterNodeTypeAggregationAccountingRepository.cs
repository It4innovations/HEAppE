using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterNodeTypeAggregationAccountingRepository
{
    IList<ClusterNodeTypeAggregationAccounting> GetAll();
    Task<IList<ClusterNodeTypeAggregationAccounting>> GetAllAsync();

    ClusterNodeTypeAggregationAccounting GetById(long clusterNodeTypeAggregationId, long accountingId);
    Task<ClusterNodeTypeAggregationAccounting> GetByIdAsync(long clusterNodeTypeAggregationId, long accountingId);

    ClusterNodeTypeAggregationAccounting
        GetByIdIncludeSoftDeleted(long clusterNodeTypeAggregationId, long accountingId);
    Task<ClusterNodeTypeAggregationAccounting>
        GetByIdIncludeSoftDeletedAsync(long clusterNodeTypeAggregationId, long accountingId);

    void Insert(ClusterNodeTypeAggregationAccounting entity);
    void Update(ClusterNodeTypeAggregationAccounting entity);
}
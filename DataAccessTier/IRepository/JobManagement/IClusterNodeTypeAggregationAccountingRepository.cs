using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterNodeTypeAggregationAccountingRepository
{
    ClusterNodeTypeAggregationAccounting GetById(long clusterNodeTypeAggregationId, long accountingId);

    ClusterNodeTypeAggregationAccounting
        GetByIdIncludeSoftDeleted(long clusterNodeTypeAggregationId, long accountingId);

    void Insert(ClusterNodeTypeAggregationAccounting entity);
    void Update(ClusterNodeTypeAggregationAccounting entity);
}
using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterNodeTypeAggregationAccountingRepository
{
    IList<ClusterNodeTypeAggregationAccounting> GetAll();

    ClusterNodeTypeAggregationAccounting GetById(long clusterNodeTypeAggregationId, long accountingId);

    ClusterNodeTypeAggregationAccounting
        GetByIdIncludeSoftDeleted(long clusterNodeTypeAggregationId, long accountingId);

    void Insert(ClusterNodeTypeAggregationAccounting entity);
    void Update(ClusterNodeTypeAggregationAccounting entity);
}
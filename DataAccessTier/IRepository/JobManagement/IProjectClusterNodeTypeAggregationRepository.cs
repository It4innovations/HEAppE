using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement
{
    public interface IProjectClusterNodeTypeAggregationRepository
    {
        ProjectClusterNodeTypeAggregation GetById(long projectId, long clusterNodeTypeAggregationId);
        List<ProjectClusterNodeTypeAggregation> GetAllByProjectId(long projectId);
        ProjectClusterNodeTypeAggregation GetByIdIncludeSoftDeleted(long projectId, long clusterNodeTypeAggregationId);
        void Insert(ProjectClusterNodeTypeAggregation entity);
        void Update(ProjectClusterNodeTypeAggregation entity);
    }
}

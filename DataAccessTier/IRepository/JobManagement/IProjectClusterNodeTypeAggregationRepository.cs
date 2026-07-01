using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IProjectClusterNodeTypeAggregationRepository
{
    ProjectClusterNodeTypeAggregation GetById(long projectId, long clusterNodeTypeAggregationId);
    Task<ProjectClusterNodeTypeAggregation> GetByIdAsync(long projectId, long clusterNodeTypeAggregationId);
    List<ProjectClusterNodeTypeAggregation> GetAllByProjectId(long projectId);
    Task<List<ProjectClusterNodeTypeAggregation>> GetAllByProjectIdAsync(long projectId);
    ProjectClusterNodeTypeAggregation GetByIdIncludeSoftDeleted(long projectId, long clusterNodeTypeAggregationId);
    Task<ProjectClusterNodeTypeAggregation> GetByIdIncludeSoftDeletedAsync(long projectId, long clusterNodeTypeAggregationId);
    void Insert(ProjectClusterNodeTypeAggregation entity);
    void Update(ProjectClusterNodeTypeAggregation entity);
}
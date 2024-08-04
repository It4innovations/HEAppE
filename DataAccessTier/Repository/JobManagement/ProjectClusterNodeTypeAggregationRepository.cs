using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class ProjectClusterNodeTypeAggregationRepository : IProjectClusterNodeTypeAggregationRepository
    {
        #region Instances
        protected readonly MiddlewareContext _context;
        protected readonly DbSet<ProjectClusterNodeTypeAggregation> _dbSet;
        #endregion
        #region Constructors
        internal ProjectClusterNodeTypeAggregationRepository(MiddlewareContext context)
        {
            _context = context;
            _dbSet = context.Set<ProjectClusterNodeTypeAggregation>();
        }

        public ProjectClusterNodeTypeAggregation GetById(long projectId, long clusterNodeTypeAggregationId)
        {
            return _dbSet.Find(projectId, clusterNodeTypeAggregationId);
        }

        public List<ProjectClusterNodeTypeAggregation> GetAllByProjectId(long projectId)
        {
            return _dbSet.Where(c => c.ProjectId == projectId).ToList();
        }

        public ProjectClusterNodeTypeAggregation GetByIdIncludeSoftDeleted(long projectId, long clusterNodeTypeAggregationId)
        {
            return _dbSet.IgnoreQueryFilters().FirstOrDefault(c => c.ProjectId == projectId && c.ClusterNodeTypeAggregationId == clusterNodeTypeAggregationId);
        }

        public void Insert(ProjectClusterNodeTypeAggregation entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(ProjectClusterNodeTypeAggregation entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }
        #endregion
    }
}

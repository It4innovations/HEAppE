using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation
{
    internal class ClusterRepository : GenericRepository<Cluster>, IClusterRepository
    {
        #region Constructors
        internal ClusterRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get Cluster with reference to project
        /// </summary>
        /// <param name="clusterId"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public Cluster GetClusterForProject(long clusterId, long projectId)
        {
            var clusterProject = _context.ClusterProjects.FirstOrDefault(p => p.ClusterId == clusterId && p.ProjectId == projectId);
            return clusterProject.Cluster;
        }

        /// <summary>
        /// Get all clusters with cluster nodes and defined command templates only with active project
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Cluster> GetAllWithActiveProjectFilter()
        {
            return GetAll().Select(c => GetCluster(c)).ToList();
        }
        #endregion

        #region Private Methods
        private Cluster GetCluster(Cluster cluster)
        {
            return new Cluster()
            {
                Id = cluster.Id,
                Name = cluster.Name,
                Description = cluster.Description,
                ClusterProjects = cluster.ClusterProjects,
                ConnectionProtocol = cluster.ConnectionProtocol,
                DomainName = cluster.DomainName,
                FileTransferMethods = cluster.FileTransferMethods,
                MasterNodeName = cluster.MasterNodeName,
                Port = cluster.Port,
                ProxyConnection = cluster.ProxyConnection,
                ProxyConnectionId = cluster.ProxyConnectionId,
                SchedulerType = cluster.SchedulerType,
                TimeZone = cluster.TimeZone,
                UpdateJobStateByServiceAccount = cluster.UpdateJobStateByServiceAccount,
                NodeTypes = cluster.NodeTypes.Select(n => GetClusterNodeType(n)).ToList()
            };
        }

        private ClusterNodeType GetClusterNodeType(ClusterNodeType n)
        {
            return new ClusterNodeType()
            {
                Id = n.Id,
                Name = n.Name,
                ClusterId = n.ClusterId,
                Cluster = n.Cluster,
                ClusterAllocationName = n.ClusterAllocationName,
                CoresPerNode = n.CoresPerNode,
                Description = n.Description,
                FileTransferMethod = n.FileTransferMethod,
                FileTransferMethodId = n.FileTransferMethodId,
                MaxWalltime = n.MaxWalltime,
                NumberOfNodes = n.NumberOfNodes,
                Queue = n.Queue,
                RequestedNodeGroups = n.RequestedNodeGroups,
                PossibleCommands = n.PossibleCommands.Where(p => p.ProjectId == null || (!p.Project.IsDeleted && p.Project.EndDate >= System.DateTime.UtcNow)).ToList()
            };
        }
        #endregion
    }
}
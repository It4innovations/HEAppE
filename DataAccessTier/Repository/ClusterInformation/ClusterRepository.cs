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
        /// Get all clusters with cluster nodes and defined command templates only with active project
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Cluster> GetAllWithActiveProjectFilter()
        {
            return _dbSet.Where(c => !c.IsDeleted).ToList().Select(c => GetCluster(c)).ToList();
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
                FileTransferMethods = cluster.FileTransferMethods.Where(ftm => !ftm.IsDeleted).ToList(),
                MasterNodeName = cluster.MasterNodeName,
                Port = cluster.Port,
                ProxyConnection = cluster.ProxyConnection == null || cluster.ProxyConnection.IsDeleted ? null : cluster.ProxyConnection,
                ProxyConnectionId = cluster.ProxyConnection == null || cluster.ProxyConnection.IsDeleted ? null : cluster.ProxyConnectionId,
                SchedulerType = cluster.SchedulerType,
                TimeZone = cluster.TimeZone,
                UpdateJobStateByServiceAccount = cluster.UpdateJobStateByServiceAccount,
                NodeTypes = cluster.NodeTypes.Where(nt => !nt.IsDeleted).Select(n => GetClusterNodeType(n)).ToList()
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
                FileTransferMethod = n.FileTransferMethod == null || n.FileTransferMethod.IsDeleted ? null : n.FileTransferMethod,
                FileTransferMethodId = n.FileTransferMethod == null || n.FileTransferMethod.IsDeleted ? null : n.FileTransferMethodId,
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
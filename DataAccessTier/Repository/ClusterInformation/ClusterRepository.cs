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

        #region Methods
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
            return GetAll().Select(c => new Cluster()
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ClusterProjects = c.ClusterProjects,
                ConnectionProtocol = c.ConnectionProtocol,
                DomainName = c.DomainName,
                FileTransferMethods = c.FileTransferMethods,
                MasterNodeName = c.MasterNodeName,
                Port = c.Port,
                ProxyConnection = c.ProxyConnection,
                ProxyConnectionId = c.ProxyConnectionId,
                SchedulerType = c.SchedulerType,
                TimeZone = c.TimeZone,
                UpdateJobStateByServiceAccount = c.UpdateJobStateByServiceAccount,
                NodeTypes = c.NodeTypes.Select(n => new ClusterNodeType()
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
                    PossibleCommands = n.PossibleCommands.Where(p => !p.Project.IsDeleted && p.Project.EndDate >= System.DateTime.UtcNow).ToList()
                }).ToList()
            });
        }
        #endregion
    }
}
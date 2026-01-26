using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation;

public interface IClusterRepository : IRepository<Cluster>
{
    IEnumerable<Cluster> GetAllWithActiveProjectFilter();
    IEnumerable<Cluster> GetAllByClusterProxyConnectionId(long clusterProxyConnectionId);
    Cluster GetByIdWithProxyConnection(long id);
    IQueryable<Cluster> AsQueryable();
}
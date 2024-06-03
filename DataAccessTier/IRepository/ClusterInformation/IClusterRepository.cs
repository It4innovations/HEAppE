using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation
{
    public interface IClusterRepository : IRepository<Cluster>
    {
        IEnumerable<Cluster> GetAllWithActiveProjectFilter();
        IEnumerable<Cluster> GetAllByClusterProxyConnectionId(long clusterProxyConnectionId);
    }
}
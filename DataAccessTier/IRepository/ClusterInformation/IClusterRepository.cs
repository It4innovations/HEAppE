using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation;

public interface IClusterRepository : IRepository<Cluster>
{
    IEnumerable<Cluster> GetAllWithActiveProjectFilter();
    IEnumerable<Cluster> GetAllByClusterProxyConnectionId(long clusterProxyConnectionId);
}
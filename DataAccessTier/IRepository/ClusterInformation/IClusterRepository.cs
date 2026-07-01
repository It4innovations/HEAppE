using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation;

public interface IClusterRepository : IRepository<Cluster>
{
    IEnumerable<Cluster> GetAllWithActiveProjectFilter();
    Task<IEnumerable<Cluster>> GetAllWithActiveProjectFilterAsync();
    IEnumerable<Cluster> GetAllByClusterProxyConnectionId(long clusterProxyConnectionId);
    Task<IEnumerable<Cluster>> GetAllByClusterProxyConnectionIdAsync(long clusterProxyConnectionId);
    Cluster GetByIdWithProxyConnection(long id);
    Task<Cluster> GetByIdWithProxyConnectionAsync(long id);
    IQueryable<Cluster> AsQueryable();
    Task<IEnumerable<Cluster>> GetClustersFilteredAsync(string clusterName, List<long> projectIds);
}
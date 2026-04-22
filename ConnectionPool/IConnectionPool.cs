using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IConnectionPool
{
    Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster,
        string sshCaToken, string lexisToken);
    Task ReturnConnectionAsync(ConnectionInfo schedulerConnection);
}
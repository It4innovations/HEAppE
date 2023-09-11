using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool
{
    public interface IConnectionPool
    {
        ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster);
        void ReturnConnection(ConnectionInfo schedulerConnection);
    }
}
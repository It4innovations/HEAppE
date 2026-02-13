using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IConnectionPool
{
    ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster,
        string sshCaToken, string lexisToken);
    void ReturnConnection(ConnectionInfo schedulerConnection);
}
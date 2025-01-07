using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IPoolableAdapter
{
    object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials,
        ClusterProxyConnection proxy, int? port);

    void Connect(object connection);

    void Disconnect(object connection);
}
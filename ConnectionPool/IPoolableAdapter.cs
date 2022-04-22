using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool
{
    public interface IPoolableAdapter
    {
        object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials, ClusterProxyConnection proxy = null, int? port = null);

        void Connect(object connection);

        void Disconnect(object connection);
    }
}
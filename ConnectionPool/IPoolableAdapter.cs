using System;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IPoolableAdapter
{
    
    object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials,
        ClusterProxyConnection proxy, string sshCaToken, int? port);

    void Connect(object connection);

    void Disconnect(object connection);
    
    bool IsConnected(object connection);
}
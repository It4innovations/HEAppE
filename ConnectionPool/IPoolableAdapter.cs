using System;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IPoolableAdapter
{
    object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials,
        Cluster cluster, string sshCaToken, string lexisToken, int? port);

    void Connect(object connection);

    void Disconnect(object connection);
    
    bool IsConnected(object connection);
}
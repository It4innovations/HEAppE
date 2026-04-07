using System;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IPoolableAdapter
{
    Task<object> CreateConnectionObjectAsync(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials,
        Cluster cluster, string sshCaToken, string lexisToken, int? port);

    Task ConnectAsync(object connection);
    Task DisconnectAsync(object connection);
    
    bool IsConnected(object connection);
}
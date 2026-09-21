using System;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool;

public interface IConnectionPool : IDisposable
{
    Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster,
        string sshCaToken, string lexisToken, Func<Task<string>>? refreshSshCaToken = null);
    Task ReturnConnectionAsync(ConnectionInfo schedulerConnection);
}
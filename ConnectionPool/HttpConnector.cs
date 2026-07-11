using System;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool
{
    public class HttpConnector : IPoolableAdapter
    {
        public Task<object> CreateConnectionObjectAsync(string masterNodeName, ClusterAuthenticationCredentials clusterCredentials,
            Cluster cluster, string sshCaToken, string lexisToken, int? port)
        {
            var protocol = cluster.ConnectionProtocol == ClusterConnectionProtocol.Https ? "https" : "http";
            int resolvedPort = 4300;
            if (cluster.CustomConfiguration != null &&
                cluster.CustomConfiguration.TryGetValue("QSchedulerPort", out var portStr) &&
                int.TryParse(portStr, out var customPort))
            {
                resolvedPort = customPort;
            }
            else
            {
                resolvedPort = port ?? cluster.Port ?? 4300;
            }
            var baseUri = $"{protocol}://{masterNodeName}:{resolvedPort}";
            return Task.FromResult<object>(new HttpConnection(baseUri));
        }

        public Task ConnectAsync(object connection) => Task.CompletedTask;
        public Task DisconnectAsync(object connection) => Task.CompletedTask;
        public bool IsConnected(object connection) => true;
    }

    public class HttpConnection
    {
        public string BaseUri { get; }
        public HttpConnection(string baseUri)
        {
            BaseUri = baseUri;
        }
    }
}

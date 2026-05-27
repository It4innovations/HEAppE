using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

public sealed class SshTunnelUtils
{
    private static readonly HashSet<int?> _usedLocalPorts = new();
    private static readonly Dictionary<long, Dictionary<string, List<TunnelInfo>>> _jobUsedPorts = new();
    private static readonly object _lock = new object();

    public SshTunnelUtils()
    {
    }

    private SshClient GetSshClient(object connectorClient)
    {
        if (connectorClient is SshClient directClient) return directClient;
        if (connectorClient is HEAppE.ConnectionPool.ConnectionInfo poolInfo && poolInfo.Connection is SshClient pooledClient) return pooledClient;
        throw new InvalidCastException("connectorClient is not an SshClient or valid ConnectionInfo");
    }

    public async Task CreateTunnelAsync(object connectorClient, long taskId, string nodeHost, int nodePort)
    {
        var sshClient = GetSshClient(connectorClient);
        TunnelInfo sshTunnelInfo;

        lock (_lock)
        {
            var localPort = GetFirstFreePort();
            var forwPort = new ForwardedPortLocal(TunnelConfiguration.LocalhostName, (uint)localPort, nodeHost, (uint)nodePort);
            sshClient.AddForwardedPort(forwPort);
            forwPort.Exception += (sender, e) => {
                System.Diagnostics.Trace.TraceError($"[SshTunnelUtils] SSH Port Forwarding Exception on local port {localPort} (Task {taskId}, Node {nodeHost}:{nodePort}): {e.Exception}");
            };
            
            sshTunnelInfo = new TunnelInfo(localPort, nodePort, nodeHost, forwPort);
            _usedLocalPorts.Add(localPort);

            if (!_jobUsedPorts.ContainsKey(taskId))
            {
                _jobUsedPorts.Add(taskId, new Dictionary<string, List<TunnelInfo>> { { nodeHost, new List<TunnelInfo> { sshTunnelInfo } } });
            }
            else
            {
                var allocatedAddressWithPorts = _jobUsedPorts[taskId];
                if (!allocatedAddressWithPorts.ContainsKey(nodeHost))
                {
                    allocatedAddressWithPorts.Add(nodeHost, new List<TunnelInfo> { sshTunnelInfo });
                }
                else
                {
                    allocatedAddressWithPorts[nodeHost].Add(sshTunnelInfo);
                }
            }
        }

        // Move the blocking network call outside the lock and into a Task
        await Task.Run(() => sshTunnelInfo.ForwardedPort.Start());
    }

    public async Task RemoveTunnelAsync(object connectorClient, long taskId)
    {
        List<TunnelInfo> tunnelsToRemove = null;
        var sshClient = GetSshClient(connectorClient);

        lock (_lock)
        {
            if (_jobUsedPorts.TryGetValue(taskId, out var nodeTunnels))
            {
                tunnelsToRemove = nodeTunnels.Values.SelectMany(t => t).ToList();
                _jobUsedPorts.Remove(taskId);
            }
        }

        if (tunnelsToRemove != null)
        {
            foreach (var s in tunnelsToRemove)
            {
                try
                {
                    await Task.Run(() => s.ForwardedPort.Stop());
                    lock (_lock)
                    {
                        sshClient.RemoveForwardedPort(s.ForwardedPort);
                        _usedLocalPorts.Remove(s.LocalPort);
                    }
                }
                catch { }
            }
        }
    }

    public IEnumerable<TunnelInfo> GetTunnelsInformations(long taskId, string nodeHost)
    {
        lock (_lock)
        {
            return _jobUsedPorts.ContainsKey(taskId) && _jobUsedPorts[taskId].ContainsKey(nodeHost)
                ? _jobUsedPorts[taskId][nodeHost].ToList()
                : Enumerable.Empty<TunnelInfo>();
        }
    }



    private static int GetFirstFreePort()
    {
        for (var port = TunnelConfiguration.MinLocalPort; port < TunnelConfiguration.MaxLocalPort; port++)
        {
            if (_usedLocalPorts.Contains(port)) continue;
            if (IsLocalPortFree(port)) return port;
        }
        throw new UnableToCreateTunnelException("NoFreeLocalPortForSsh");
    }

    private static bool IsLocalPortFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Parse(TunnelConfiguration.LocalhostName), port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
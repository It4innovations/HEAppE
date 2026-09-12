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

    private SshClient? GetSshClient(object? connectorClient)
    {
        if (connectorClient == null) return null;
        if (connectorClient is SshClient directClient) return directClient;
        if (connectorClient is HEAppE.ConnectionPool.ConnectionInfo poolInfo && poolInfo.Connection is SshClient pooledClient) return pooledClient;
        throw new InvalidCastException("connectorClient is not an SshClient or valid ConnectionInfo");
    }

    public async Task CreateTunnelAsync(object connectorClient, long taskId, string nodeHost, int nodePort)
    {
        var sshClient = GetSshClient(connectorClient) ?? throw new ArgumentNullException(nameof(connectorClient));
        TunnelInfo sshTunnelInfo;
        int localPort;
        ForwardedPortLocal forwPort;

        lock (_lock)
        {
            localPort = GetFirstFreePort();
            forwPort = new ForwardedPortLocal(TunnelConfiguration.LocalhostName, (uint)localPort, nodeHost, (uint)nodePort);
            sshClient.AddForwardedPort(forwPort);
            forwPort.Exception += OnForwardedPortException;
            
            sshTunnelInfo = new TunnelInfo(localPort, nodePort, nodeHost, forwPort, sshClient);
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
        try
        {
            await Task.Run(() => sshTunnelInfo.ForwardedPort.Start());
        }
        catch
        {
            lock (_lock)
            {
                try { sshClient.RemoveForwardedPort(forwPort); } catch { }
                _usedLocalPorts.Remove(localPort);
                if (_jobUsedPorts.TryGetValue(taskId, out var allocatedAddressWithPorts))
                {
                    if (allocatedAddressWithPorts.TryGetValue(nodeHost, out var list))
                    {
                        list.Remove(sshTunnelInfo);
                        if (list.Count == 0) allocatedAddressWithPorts.Remove(nodeHost);
                    }
                    if (allocatedAddressWithPorts.Count == 0) _jobUsedPorts.Remove(taskId);
                }
            }
            try { forwPort.Exception -= OnForwardedPortException; } catch { }
            try { forwPort.Dispose(); } catch { }
            throw;
        }
    }

    private static void OnForwardedPortException(object? sender, Renci.SshNet.Common.ExceptionEventArgs e)
    {
        if (sender is ForwardedPortLocal p)
        {
            System.Diagnostics.Trace.TraceError($"[SshTunnelUtils] SSH Port Forwarding Exception on local port {p.Port} (Target {p.Host}:{p.Port}): {e.Exception}");
        }
        else
        {
            System.Diagnostics.Trace.TraceError($"[SshTunnelUtils] SSH Port Forwarding Exception: {e.Exception}");
        }
    }

    public async Task RemoveTunnelAsync(object? connectorClient, long taskId)
    {
        List<TunnelInfo>? tunnelsToRemove = null;
        SshClient? explicitClient = null;
        if (connectorClient != null)
        {
            try { explicitClient = GetSshClient(connectorClient); } catch { }
        }

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
                }
                catch { }

                try
                {
                    s.ForwardedPort.Exception -= OnForwardedPortException;
                }
                catch { }

                try
                {
                    var client = explicitClient ?? s.SshClient;
                    client?.RemoveForwardedPort(s.ForwardedPort);
                }
                catch { }

                lock (_lock)
                {
                    _usedLocalPorts.Remove(s.LocalPort);
                }

                try
                {
                    s.ForwardedPort.Dispose();
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
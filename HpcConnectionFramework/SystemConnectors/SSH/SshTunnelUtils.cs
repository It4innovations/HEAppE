using System;
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

    public void CreateTunnel(object connectorClient, long taskId, string nodeHost, int nodePort)
    {
        lock (_lock)
        {
            var sshClient = GetSshClient(connectorClient);
            
            if (_jobUsedPorts.ContainsKey(taskId))
            {
                var allocatedAddressWithPorts = _jobUsedPorts[taskId];
                if (allocatedAddressWithPorts.ContainsKey(nodeHost))
                {
                    var allocatedPortsForJob = allocatedAddressWithPorts[nodeHost];
                    var sshTunnelInfo = CreateSshTunnel(sshClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                    allocatedPortsForJob.Add(sshTunnelInfo);
                }
                else
                {
                    var sshTunnelInfo = CreateSshTunnel(sshClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                    allocatedAddressWithPorts.Add(nodeHost, new List<TunnelInfo> { sshTunnelInfo });
                }
            }
            else
            {
                var sshTunnelInfo = CreateSshTunnel(sshClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                _jobUsedPorts.Add(taskId, new Dictionary<string, List<TunnelInfo>> { { nodeHost, new List<TunnelInfo> { sshTunnelInfo } } });
            }
        }
    }

    public void RemoveTunnel(object connectorClient, long taskId)
    {
        lock (_lock)
        {
            if (!_jobUsedPorts.ContainsKey(taskId))
                throw new UnableToCreateTunnelException("NoActiveTunnel", taskId);

            var sshClient = GetSshClient(connectorClient);

            foreach (var nodeAddress in _jobUsedPorts[taskId].Keys)
            {
                foreach (var s in _jobUsedPorts[taskId][nodeAddress])
                {
                    try
                    {
                        s.ForwardedPort.Stop();
                        sshClient.RemoveForwardedPort(s.ForwardedPort);
                        _usedLocalPorts.Remove(s.LocalPort);
                    }
                    catch { }
                }
            }
            _jobUsedPorts.Remove(taskId);
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

    private static TunnelInfo CreateSshTunnel(SshClient sshClient, string localHost, int localPort, string nodeHost, int nodePort)
    {
        var forwPort = new ForwardedPortLocal(localHost, (uint)localPort, nodeHost, (uint)nodePort);
        sshClient.AddForwardedPort(forwPort);
        forwPort.Exception += (sender, e) => throw new UnableToCreateTunnelException("ExceptionOccurs", e.Exception);

        forwPort.Start();
        _usedLocalPorts.Add(localPort);

        return new TunnelInfo(localPort, nodePort, nodeHost, forwPort);
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
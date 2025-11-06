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

/// <summary>
///     SSH tunnels
/// </summary>
public sealed class SshTunnelUtils
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public SshTunnelUtils()
    {
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Used local ports
    /// </summary>
    private static readonly HashSet<int?> _usedLocalPorts = new();

    /// <summary>
    ///     Created tunnels for job
    /// </summary>
    private static readonly Dictionary<long, Dictionary<string, List<TunnelInfo>>> _jobUsedPorts = new();

    #endregion

    #region Local Methods

    /// <summary>
    ///     Create tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskId">Task id</param>
    /// <param name="nodeHost">Node address</param>
    /// <param name="nodePort">Node port</param>
    public void CreateTunnel(object connectorClient, long taskId, string nodeHost, int nodePort)
    {
        if (_jobUsedPorts.ContainsKey(taskId))
        {
            var allocatedAddressWithPorts = _jobUsedPorts[taskId];
            if (allocatedAddressWithPorts.ContainsKey(nodeHost))
            {
                var allocatedPortsForJob = allocatedAddressWithPorts[nodeHost];
                
                //if contains already allocated then thow, or if nodePort is not allocated then create tunnel
                if (allocatedPortsForJob.Any(f => f.RemotePort == nodePort))
                    throw new UnableToCreateConnectionException("PortAlreadyInUse", taskId, nodeHost, nodePort);

                var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName,
                    GetFirstFreePort(), nodeHost, nodePort);
                allocatedPortsForJob.Add(sshTunnelInfo);
            }
            else
            {
                var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName,
                    GetFirstFreePort(), nodeHost, nodePort);
                allocatedAddressWithPorts.Add(nodeHost, new List<TunnelInfo> { sshTunnelInfo });
            }
        }
        else
        {
            var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(),
                nodeHost, nodePort);
            _jobUsedPorts.Add(taskId,
                new Dictionary<string, List<TunnelInfo>> { { nodeHost, new List<TunnelInfo> { sshTunnelInfo } } });
        }
    }

    /// <summary>
    ///     Remove SSH tunnel
    /// </summary>
    /// <param name="connectorClient">Connector</param>
    /// <param name="taskId">Task Id</param>
    public void RemoveTunnel(object connectorClient, long taskId)
    {
        if (!_jobUsedPorts.ContainsKey(taskId))
            throw new UnableToCreateTunnelException("NoActiveTunnel", taskId);

        var sshClient = ((HEAppE.ConnectionPool.ConnectionInfo)connectorClient).Connection as SshClient;

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
                catch (Exception ex)
                {
                    // ignored
                }
            }
        }

        _jobUsedPorts.Remove(taskId);
    }


    /// <summary>
    ///     Get tunnels informations
    /// </summary>
    /// <param name="taskId">Task Id</param>
    /// <param name="nodeHost">Node host</param>
    /// <returns></returns>
    public IEnumerable<TunnelInfo> GetTunnelsInformations(long taskId, string nodeHost)
    {
        return _jobUsedPorts.ContainsKey(taskId) && _jobUsedPorts[taskId].ContainsKey(nodeHost)
            ? _jobUsedPorts[taskId][nodeHost]
            : Enumerable.Empty<TunnelInfo>();
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Create SSH tunnel
    /// </summary>
    /// <param name="connectorClient">Schduler</param>
    /// <param name="localHost">Local host</param>
    /// <param name="localPort">Local port</param>
    /// <param name="nodeHost">Node host</param>
    /// <param name="nodePort">Node port</param>
    /// <returns></returns>
    private static TunnelInfo CreateSshTunnel(object connectorClient, string localHost, int localPort, string nodeHost,
        int nodePort)
    {
        var sshClient = (SshClient)connectorClient;

        var forwPort = new ForwardedPortLocal(localHost, (uint)localPort, nodeHost, (uint)nodePort);
        sshClient.AddForwardedPort(forwPort);
        forwPort.Exception += (sender, e) => throw new UnableToCreateTunnelException("ExceptionOccurs", e.Exception);

        forwPort.Start();
        _usedLocalPorts.Add(localPort);

        return new TunnelInfo(localPort, nodePort, nodeHost, forwPort);
    }

    /// <summary>
    ///     Get first free local port
    /// </summary>
    /// <returns></returns>
    private static int GetFirstFreePort()
    {
        for (var port = TunnelConfiguration.MinLocalPort; 
             port < TunnelConfiguration.MaxLocalPort; 
             port++)
        {
            if (_usedLocalPorts.Contains(port))
                continue;

            if (IsLocalPortFree(port))
                return port;
        }

        throw new UnableToCreateTunnelException("NoFreeLocalPortForSsh");
    }


    /// <summary>
    ///     Validate if port is free
    /// </summary>
    /// <param name="port">Port</param>
    /// <returns></returns>
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

    #endregion
}
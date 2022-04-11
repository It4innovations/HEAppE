using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.Exceptions;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// SSH tunnels
    /// </summary>
    public sealed class SshTunnelUtils
    {
        #region Properties
        /// <summary>
        /// Used local ports
        /// </summary>
        private static readonly HashSet<int?> _usedLocalPorts = new();

        /// <summary>
        /// Created tunnels for job
        /// </summary>
        private static readonly Dictionary<long, Dictionary<string, List<TunnelInfo>>> _jobUsedPorts = new();
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public SshTunnelUtils()
        {

        }
        #endregion
        #region Local Methods
        /// <summary>
        /// Create tunnel
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
                    if (allocatedPortsForJob.FirstOrDefault(f => f.RemotePort == nodePort).LocalPort is null)
                    {
                        throw new UnableToCreateTunnelException($"Task id: \"{taskId}\" with node IP address: \"{nodeHost}\" already has ssh tunnel for port: \"{nodePort}\".");
                    }
                    else
                    {
                        var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                        allocatedPortsForJob.Add(sshTunnelInfo);
                    }
                }
                else
                {
                    var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                    allocatedAddressWithPorts.Add(nodeHost, new List<TunnelInfo> { sshTunnelInfo });
                }
            }
            else
            {
                var sshTunnelInfo = CreateSshTunnel(connectorClient, TunnelConfiguration.LocalhostName, GetFirstFreePort(), nodeHost, nodePort);
                _jobUsedPorts.Add(taskId, new Dictionary<string, List<TunnelInfo>> { { nodeHost, new List<TunnelInfo>() { sshTunnelInfo } } });
            }
        }

        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="connectorClient">Connector</param>
        /// <param name="taskId">Task Id</param>
        public void RemoveTunnel(object connectorClient, long taskId)
        {
            if (_jobUsedPorts.ContainsKey(taskId))
            {
                foreach (var nodeAddress in _jobUsedPorts[taskId].Keys)
                {
                    foreach (var s in _jobUsedPorts[taskId][nodeAddress])
                    {
                        _usedLocalPorts.Remove(s.LocalPort);

                        s.ForwardedPort.Stop();
                    }
                    var localPorts = _jobUsedPorts[taskId][nodeAddress].Select(s => _usedLocalPorts.Remove(s.LocalPort));
                }
                _jobUsedPorts.Remove(taskId);
            }
            else
            {
                throw new UnableToCreateTunnelException("Task \"{taskId}\" does not have an active ssh tunnel.");
            }
        }

        /// <summary>
        /// Get tunnels informations
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
        /// Create SSH tunnel
        /// </summary>
        /// <param name="connectorClient">Schduler</param>
        /// <param name="localHost">Local host</param>
        /// <param name="localPort">Local port</param>
        /// <param name="nodeHost">Node host</param>
        /// <param name="nodePort">Node port</param>
        /// <returns></returns>
        private static TunnelInfo CreateSshTunnel(object connectorClient, string localHost, int localPort, string nodeHost, int nodePort)
        {
            var sshClient = (SshClient)connectorClient;

            var forwPort = new ForwardedPortLocal(localHost, (uint)localPort, nodeHost, (uint)nodePort);
            sshClient.AddForwardedPort(forwPort);
            forwPort.Exception += (sender, e) => throw new UnableToCreateTunnelException("Exception occuers during creation SSH tunnel", e.Exception);

            forwPort.Start();
            _usedLocalPorts.Add(localPort);

            return new TunnelInfo(localPort, nodePort, nodeHost, forwPort);
        }
        /// <summary>
        /// Get first free local port
        /// </summary>
        /// <returns></returns>
        private static int GetFirstFreePort()
        {
            int port = TunnelConfiguration.MinLocalPort;
            do
            {
                var last = _usedLocalPorts.LastOrDefault();
                if (last is not null)
                {
                    if ((last + 1) < TunnelConfiguration.MaxLocalPort)
                    {
                        port = last.Value + 1;
                    }
                    else
                    {
                        for (int i = TunnelConfiguration.MinLocalPort; i < TunnelConfiguration.MaxLocalPort; i++)
                        {
                            if (!_usedLocalPorts.Contains(i))
                            {
                                port = i;
                                break;
                            }
                            else
                            {
                                throw new UnableToCreateTunnelException("There is not free local port for creation ssh tunnel.");
                            }
                        }
                    }
                }
            }
            while (IsLocalPortFree(port));
            return port;
        }

        /// <summary>
        /// Validate if port is free
        /// </summary>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static bool IsLocalPortFree(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Parse(TunnelConfiguration.LocalhostName), port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }
        #endregion
    }
}

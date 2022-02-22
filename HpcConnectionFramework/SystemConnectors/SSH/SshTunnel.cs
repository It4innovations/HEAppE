using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// SSH tunnels
    /// </summary>
    internal sealed class SshTunnel
    {
        #region Properties
        /// <summary>
        /// Job SSH tunnels
        /// </summary>
        private static Dictionary<long, Dictionary<string, SshClient>> _jobHostTunnels;

        /// <summary>
        /// Log4Net logger
        /// </summary>
        private ILog _log;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        internal SshTunnel()
        {
            _log = LogManager.GetLogger(typeof(SshTunnel));
            _jobHostTunnels = new Dictionary<long, Dictionary<string, SshClient>>();
        }
        #endregion
        #region Methods
        /// <summary>
        /// Create SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="localHost">Local host</param>
        /// <param name="localPort">Local port</param>
        /// <param name="loginHost">Login host</param>
        /// <param name="nodeHost">Node host</param>
        /// <param name="nodePort">Node port</param>
        /// <param name="credentials">Credentials</param>
        internal void CreateSshTunnel(long jobId, string localHost, int localPort, string loginHost, string nodeHost, int nodePort, ClusterAuthenticationCredentials credentials)
        {
            if (_jobHostTunnels.ContainsKey(jobId) && _jobHostTunnels[jobId].ContainsKey(nodeHost))
            {
                _log.ErrorFormat("JobId {0} with IPaddress {1} already has a socket connection.", jobId, nodeHost);
            }
            else
            {
                SshClient sshTunnel = new SshClient(loginHost, credentials.Username, new PrivateKeyFile(credentials.PrivateKeyFile, credentials.PrivateKeyPassword));
                sshTunnel.Connect();

                var forwPort = new ForwardedPortLocal(localHost, (uint)localPort, nodeHost, (uint)nodePort);
                sshTunnel.AddForwardedPort(forwPort);

                forwPort.Exception += delegate (object sender, ExceptionEventArgs e)
                {
                    _log.Error(e.Exception.ToString());
                };

                forwPort.Start();

                if (_jobHostTunnels.ContainsKey(jobId))
                {
                    _jobHostTunnels[jobId].Add(nodeHost, sshTunnel);
                }
                else
                {
                    _jobHostTunnels.Add(jobId, new Dictionary<string, SshClient> { { nodeHost, sshTunnel } });
                }
                _log.InfoFormat("Ssh tunel for jobId {0} and node IPaddress {1}:{2} created.", jobId, nodeHost, nodePort);
            }
        }

        /// <summary>
        /// Remove SSH tunnel
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        internal void RemoveSshTunnel(long jobId, string nodeHost)
        {
            if (_jobHostTunnels[jobId][nodeHost] != null)
            {
                foreach (ForwardedPort port in _jobHostTunnels[jobId][nodeHost].ForwardedPorts)
                {
                    port.Stop();
                }

                _jobHostTunnels[jobId][nodeHost].Disconnect();
                _jobHostTunnels[jobId].Remove(nodeHost);

                if (_jobHostTunnels[jobId].Count == 0)
                {
                    _jobHostTunnels.Remove(jobId);
                }
                _log.InfoFormat("Ssh tunel for jobId {0} and node IPaddress {1} removed.", jobId, nodeHost);
            }
        }

        /// <summary>
        /// Check if SSH tunnel exist
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <param name="nodeHost">Node host</param>
        /// <returns></returns>
        internal bool SshTunnelExist(long jobId, string nodeHost)
        {
            return _jobHostTunnels.ContainsKey(jobId) && _jobHostTunnels[jobId].ContainsKey(nodeHost);
        }
        #endregion
    }
}

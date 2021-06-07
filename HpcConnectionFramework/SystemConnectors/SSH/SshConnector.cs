using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Globalization;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// Class: Ssh connector
    /// </summary>
    public class SshConnector : IPoolableAdapter
    {
        /// <summary>
        /// Method: Create ssh connection object
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        public object CreateConnectionObject(string masterNodeName, string remoteTimeZone, ClusterAuthenticationCredentials credentials)
        {
#warning TODO timezone
            if (!string.IsNullOrEmpty(credentials.PrivateKeyFile))
            {
                return CreateConnectionObjectUsingPrivateKeyAuthentication(masterNodeName, credentials.Username, credentials.PrivateKeyFile, credentials.PrivateKeyPassword);
            }
            else
            {
                if (!string.IsNullOrEmpty(credentials.Password))
                {
                    switch (credentials.Cluster.ConnectionProtocol)
                    {
                        case ClusterConnectionProtocol.MicrosoftHpcApi:
                            return CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password);

                        case ClusterConnectionProtocol.Ssh:
                            return CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password);

                        case ClusterConnectionProtocol.SshInteractive:
                            return CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(masterNodeName, credentials.Username, credentials.Password);

                        default:
                            return CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password);
                    }
                }
                else
                {
                    //USE ssh-agent
                    return CreateConnectionObjectUsingNoAuthentication(masterNodeName, credentials.Username);
                }
            }
        }

        /// <summary>
        /// Method: Connect client to server
        /// </summary>
        /// <param name="connectorClient"></param>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="credentials">Credentials</param>
        public void Connect(object connectorClient, string masterNodeName, ClusterAuthenticationCredentials credentials)
        {
            new SshClientAdapter((SshClient)connectorClient).Connect();
        }

        /// <summary>
        /// Method: Disconnect client from server
        /// </summary>
        /// <param name="scheduler"></param>
        public void Disconnect(object connectorClient)
        {
            new SshClientAdapter((SshClient)connectorClient).Disconnect();
        }

        /// <summary>
        /// Method: Create connection object using password authentication
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private object CreateConnectionObjectUsingPasswordAuthentication(string masterNodeName, string username, string password)
        {
            var hostPort = masterNodeName.Split(':');
            int port = int.Parse(hostPort[1]??"22");
            var connInfo = new Renci.SshNet.ConnectionInfo(hostPort[0], port, username, new PasswordAuthenticationMethod(username, password));
            return new SshClient(connInfo);
        }

        /// <summary>
        /// Method: Create connection object using password authentication with keyboard interactive
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private object CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(string masterNodeName, string username, string password)
        {
            KeyboardInteractiveConnectionInfo connectionInfo = new KeyboardInteractiveConnectionInfo(masterNodeName, username);
            connectionInfo.AuthenticationPrompt += delegate (object sender, AuthenticationPromptEventArgs e)
            {
                foreach (AuthenticationPrompt prompt in e.Prompts)
                {
                    prompt.Response = password;
                }
            };
            return new SshClient(connectionInfo);
        }
        /// <summary>
        /// Method: Create connection object using private key authentication
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="username">Username</param>
        /// <param name="privateKeyFile">Private key file</param>
        /// <param name="privateKeyPassword">Private key password</param>
        /// <returns></returns>
        private object CreateConnectionObjectUsingPrivateKeyAuthentication(string masterNodeName, string username, string privateKeyFile, string privateKeyPassword)
        {
            PrivateKeyConnectionInfo connectionInfo = new PrivateKeyConnectionInfo(
                masterNodeName,
                username,
                new PrivateKeyFile(privateKeyFile, privateKeyPassword));

            SshClient client = new SshClient(connectionInfo);
            return client;
        }

        private object CreateConnectionObjectUsingNoAuthentication(string masterNodeName, string username)
        {
            NoAuthenticationSshClient client = new NoAuthenticationSshClient(masterNodeName, username);
            return client;
        }
    }
}

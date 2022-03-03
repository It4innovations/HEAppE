using HEAppE.DomainObjects.ClusterInformation;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// Ssh connector
    /// </summary>
    public class SshConnector : ConnectionPool.IPoolableAdapter
    {
        #region Local Methods
        /// <summary>
        /// Create ssh connection object
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="credentials">Credentials</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        public object CreateConnectionObject(string masterNodeName, string remoteTimeZone, ClusterAuthenticationCredentials credentials, int? port)
        {
            return credentials.AuthenticationType switch
            {
                ClusterAuthenticationCredentialsAuthType.Password => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password, port),
                ClusterAuthenticationCredentialsAuthType.PasswordInteractive => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(masterNodeName, credentials.Username, credentials.Password),
                ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey => CreateConnectionObjectUsingPrivateKeyAndPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),
                ClusterAuthenticationCredentialsAuthType.PrivateKey => CreateConnectionObjectUsingPrivateKeyAuthentication(masterNodeName, credentials.Username, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),
                ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent => CreateConnectionObjectUsingNoAuthentication(masterNodeName, credentials.Username),
                _ => throw new NotImplementedException("Cluster authentication credentials authentication type is not allowed!")
            };
        }

        /// <summary>
        /// Connect client to server
        /// </summary>
        /// <param name="connectorClient"></param>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="credentials">Credentials</param>
        public void Connect(object connectorClient, string masterNodeName, ClusterAuthenticationCredentials credentials)
        {
            new SshClientAdapter((SshClient)connectorClient).Connect();
        }

        /// <summary>
        /// Disconnect client from server
        /// </summary>
        /// <param name="scheduler"></param>
        public void Disconnect(object connectorClient)
        {
            new SshClientAdapter((SshClient)connectorClient).Disconnect();
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Create connection object using password authentication
        /// </summary>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPasswordAuthentication(string masterNodeName, string username, string password, int? port)
        {
            var connectionInfo = port switch
            {
                null => new ConnectionInfo(
                    masterNodeName,
                    username,
                    new PasswordAuthenticationMethod(username, password)),
                _ => new ConnectionInfo(
                    masterNodeName,
                    port.Value,
                    username,
                    new PasswordAuthenticationMethod(username, password))
            };
        
            return new SshClient(connectionInfo);
        }

        /// <summary>
        /// Create connection object using password authentication with keyboard interactive
        /// </summary>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(string masterNodeName, string username, string password)
        {
            var connectionInfo = new KeyboardInteractiveConnectionInfo(masterNodeName, username);
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
        /// Create connection object using private key authentication
        /// </summary>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="privateKeyFile">Private key file</param>
        /// <param name="privateKeyPassword">Private key password</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPrivateKeyAuthentication(string masterNodeName, string username, string privateKeyFile, string privateKeyPassword, int? port)
        {
            try
            {
                PrivateKeyConnectionInfo connectionInfo = port switch
                {
                    null => new PrivateKeyConnectionInfo(
                        masterNodeName,
                        username,
                        new PrivateKeyFile(privateKeyFile, privateKeyPassword)),
                    _ => new PrivateKeyConnectionInfo(
                        masterNodeName,
                        port.Value,
                        username,
                        new PrivateKeyFile(privateKeyFile, privateKeyPassword))
                };

                var client = new SshClient(connectionInfo);
                return client;
            }
            catch (Exception e)
            {
                throw new SshCommandException($"Not corresponding password for the private key that is used for the connection to \"{masterNodeName}\"!", e);
            }
        }

        /// <summary>
        /// Create connection object using private key and password authentication
        /// </summary>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="privateKeyFile">Private key file</param>
        /// <param name="privateKeyPassword">Private key password</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPrivateKeyAndPasswordAuthentication(string masterNodeName, string username, string password, string privateKeyFile, string privateKeyPassword, int? port)
        {
            try
            {
                var connectionInfo = port switch
                {
                    null => new ConnectionInfo(
                        masterNodeName,
                        username,
                        new PasswordAuthenticationMethod(username, password),
                        new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(privateKeyFile, privateKeyPassword))),
                    _ => new ConnectionInfo(
                        masterNodeName,
                        port.Value,
                        username,
                        new PasswordAuthenticationMethod(username, password),
                        new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(privateKeyFile, privateKeyPassword)))
                };

                var client = new SshClient(connectionInfo);
                return client;
            }
            catch (Exception e)
            {
                throw new SshCommandException($"Not corresponding password for the private key that is used for the connection to \"{masterNodeName}\"!", e);
            }
        }

        /// <summary>
        /// Create connection object using private key stored in memory (ssh-agent)
        /// </summary>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingNoAuthentication(string masterNodeName, string username)
        {
            var client = new NoAuthenticationSshClient(masterNodeName, username);
            return client;
        }
        #endregion
    }
}

using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.Exceptions;
using HEAppE.Utils;
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
        /// <param name="proxy">Proxy</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        public object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials credentials, ClusterProxyConnection proxy, int? port)
        {
            return credentials.AuthenticationType switch
            {
                ClusterAuthenticationCredentialsAuthType.Password 
                        => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password, port),

                ClusterAuthenticationCredentialsAuthType.PasswordInteractive 
                        => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(masterNodeName, credentials.Username, credentials.Password),

                ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey 
                        => CreateConnectionObjectUsingPrivateKeyAndPasswordAuthentication(masterNodeName, credentials.Username, credentials.Password, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),

                ClusterAuthenticationCredentialsAuthType.PrivateKey 
                        => CreateConnectionObjectUsingPrivateKeyAuthentication(masterNodeName, credentials.Username, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),

                ClusterAuthenticationCredentialsAuthType.PasswordViaProxy
                        => CreateConnectionObjectUsingPasswordAuthenticationViaProxy(proxy.Host, proxy.Type, proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.Password, port),

                ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy
                        => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractiveViaProxy(proxy.Host, proxy.Type, proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.Password, port),

                ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy 
                        => CreateConnectionObjectUsingPrivateKeyAndPasswordAuthenticationViaProxy(proxy.Host, proxy.Type, proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.Password, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),

                ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy
                        => CreateConnectionObjectUsingPrivateKeyAuthenticationViaProxy(proxy.Host, proxy.Type, proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.PrivateKeyFile, credentials.PrivateKeyPassword, port),

                ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent
                        => CreateConnectionObjectUsingNoAuthentication(masterNodeName, credentials.Username),

                _ => throw new NotImplementedException("Cluster authentication credentials authentication type is not allowed!")
            };
        }

        /// <summary>
        /// Connect client to server
        /// </summary>
        /// <param name="connectorClient"></param>
        public void Connect(object connectorClient)
        {
            new SshClientAdapter((SshClient)connectorClient).Connect();
        }

        /// <summary>
        /// Disconnect client from server
        /// </summary>
        /// <param name="connectorClient"></param>
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
        /// <param name="port">Port</param>
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
        /// Create connection object using password authentication via Proxy
        /// </summary>
        /// <param name="proxyHost">Proxy host</param>
        /// <param name="proxyType">Proxy type</param>
        /// <param name="proxyPort">Proxy port</param>
        /// <param name="proxyUsername">Proxy username</param>
        /// <param name="proxyPassword">Proxy password</param>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPasswordAuthenticationViaProxy(string proxyHost, ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName, string username, string password, int? port)
        {
            var connectionInfo = new ConnectionInfo(
                                                 masterNodeName,
                                                 port ?? 22,
                                                 username,
                                                 proxyType.Map(),
                                                 proxyHost,
                                                 proxyPort,
                                                 proxyUsername,
                                                 proxyPassword,
                                                 new PasswordAuthenticationMethod(username, password));
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
        /// Create connection object using password authentication with keyboard interactive via Proxy
        /// </summary>
        /// <param name="proxyHost">Proxy host</param>
        /// <param name="proxyType">Proxy type</param>
        /// <param name="proxyPort">Proxy port</param>
        /// <param name="proxyUsername">Proxy username</param>
        /// <param name="proxyPassword">Proxy password</param>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractiveViaProxy(string proxyHost, ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName, string username, string password, int? port)
        {
            var connectionInfo = port switch
            {
                null => new KeyboardInteractiveConnectionInfo(
                            masterNodeName,
                            username,
                            proxyType.Map(),
                            proxyHost,
                            proxyPort,
                            proxyUsername ?? string.Empty,
                            proxyPassword ?? string.Empty),
                _ => new KeyboardInteractiveConnectionInfo(
                            masterNodeName,
                            port.Value,
                            username,
                            proxyType.Map(),
                            proxyHost,
                            proxyPort,
                            proxyUsername ?? string.Empty,
                            proxyPassword ?? string.Empty)
            };

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
        /// Create connection object using private key authentication via Proxy
        /// </summary>
        /// <param name="proxyHost">Proxy host</param>
        /// <param name="proxyType">Proxy type</param>
        /// <param name="proxyPort">Proxy port</param>
        /// <param name="proxyUsername">Proxy username</param>
        /// <param name="proxyPassword">Proxy password</param>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="privateKeyFile">Private key file</param>
        /// <param name="privateKeyPassword">Private key password</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPrivateKeyAuthenticationViaProxy(string proxyHost, ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName, string username, string privateKeyFile, string privateKeyPassword, int? port)
        {
            try
            {
                PrivateKeyConnectionInfo connectionInfo = port switch
                {
                    null => new PrivateKeyConnectionInfo(
                                masterNodeName,
                                username,
                                proxyType.Map(),
                                proxyHost,
                                proxyPort,
                                proxyUsername ?? string.Empty,
                                proxyPassword ?? string.Empty,
                                new PrivateKeyFile(privateKeyFile, privateKeyPassword)),
                    _ => new PrivateKeyConnectionInfo(
                                masterNodeName,
                                port.Value,
                                username,
                                proxyType.Map(),
                                proxyHost,
                                proxyPort,
                                proxyUsername ?? string.Empty,
                                proxyPassword ?? string.Empty,
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
        /// Create connection object using private key and password authentication via Proxy
        /// </summary>
        /// <param name="proxyHost">Proxy host</param>
        /// <param name="proxyType">Proxy type</param>
        /// <param name="proxyPort">Proxy port</param>
        /// <param name="proxyUsername">Proxy username</param>
        /// <param name="proxyPassword">Proxy password</param>
        /// <param name="masterNodeName">Master host name</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="privateKeyFile">Private key file</param>
        /// <param name="privateKeyPassword">Private key password</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        private static object CreateConnectionObjectUsingPrivateKeyAndPasswordAuthenticationViaProxy(string proxyHost, ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName, string username, string password, string privateKeyFile, string privateKeyPassword, int? port)
        {
            try
            {
                var connectionInfo = new ConnectionInfo(
                                                     masterNodeName,
                                                     port ?? 22,
                                                     username,
                                                     proxyType.Map(),
                                                     proxyHost,
                                                     proxyPort,
                                                     proxyUsername ?? string.Empty,
                                                     proxyPassword ?? string.Empty,
                                                     new PasswordAuthenticationMethod(username, password),
                                                     new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(privateKeyFile, privateKeyPassword)));

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

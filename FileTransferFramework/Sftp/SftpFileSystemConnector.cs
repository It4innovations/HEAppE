using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class SftpFileSystemConnector : IPoolableAdapter
    {
        #region Instances
        readonly ILogger _logger;
        #endregion
        #region Constructors
        public SftpFileSystemConnector(ILogger logger)
        {
            _logger = logger;
        }
        #endregion
        #region Methods
        public object CreateConnectionObject(string masterNodeName, string remoteTimeZone, ClusterAuthenticationCredentials credentials)
        {
            if (!string.IsNullOrEmpty(credentials.PrivateKeyFile))
            {
                return CreateConnectionObjectUsingPrivateKeyAuthentication(masterNodeName, remoteTimeZone, credentials.Username, credentials.PrivateKeyFile, credentials.PrivateKeyPassword);
            }
            else
            {
                if (!string.IsNullOrEmpty(credentials.Password))
                {
                    return credentials.Cluster.ConnectionProtocol switch
                    {
                        ClusterConnectionProtocol.MicrosoftHpcApi => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, remoteTimeZone, credentials.Username, credentials.Password),
                        ClusterConnectionProtocol.Ssh => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, remoteTimeZone, credentials.Username, credentials.Password),
                        ClusterConnectionProtocol.SshInteractive => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(masterNodeName, remoteTimeZone, credentials.Username, credentials.Password),
                        _ => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, remoteTimeZone, credentials.Username, credentials.Password),
                    };
                }
                else
                {
                    //Using Ssh-Agent
                    return CreateConnectionObjectUsingNoAuthentication(masterNodeName, remoteTimeZone, credentials.Username);
                }
            }
        }

        public void Connect(object connection, string masterNodeName, ClusterAuthenticationCredentials credentials)
        {
            new SftpClientAdapter((ExtendedSftpClient)connection).Connect();
        }

        public void Disconnect(object connection)
        {
            new SftpClientAdapter((ExtendedSftpClient)connection).Disconnect();
        }
        #endregion
        #region Local Methods
        private NoAuthenticationSftpClient CreateConnectionObjectUsingNoAuthentication(string masterNodeName, string remoteTimeZone, string username)
        {
            NoAuthenticationSftpClient client = new NoAuthenticationSftpClient(_logger, masterNodeName, remoteTimeZone, username);
            return client;
        }

        private static SftpClient CreateConnectionObjectUsingPasswordAuthentication(string masterNodeName, string remoteTimeZone, string username, string password)
        {
            var connInfo = new Renci.SshNet.ConnectionInfo(masterNodeName, username, new PasswordAuthenticationMethod(username, password));
            return new ExtendedSftpClient(connInfo, remoteTimeZone);
        }

        private static SftpClient CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(string masterNodeName, string remoteTimeZone, string username, string password)
        {
            KeyboardInteractiveConnectionInfo connectionInfo = new KeyboardInteractiveConnectionInfo(masterNodeName, username);
            connectionInfo.AuthenticationPrompt += delegate (object sender, AuthenticationPromptEventArgs e)
            {
                foreach (AuthenticationPrompt prompt in e.Prompts)
                {
                    prompt.Response = password;
                }
            };
            return new ExtendedSftpClient(connectionInfo, remoteTimeZone);
        }

        private static SftpClient CreateConnectionObjectUsingPrivateKeyAuthentication(string masterNodeName, string remoteTimeZone, string username, string privateKeyFile, string privateKeyPassword)
        {
            PrivateKeyConnectionInfo connectionInfo = new PrivateKeyConnectionInfo(masterNodeName, username, new PrivateKeyFile(privateKeyFile, privateKeyPassword));
            return new ExtendedSftpClient(connectionInfo, remoteTimeZone);
        }
        #endregion
    }
}
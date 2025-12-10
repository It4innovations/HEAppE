using System;
using System.IO;
using System.Text;
using HEAppE.CertificateGenerator;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshCaAPI;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace HEAppE.FileTransferFramework.Sftp;

public class SftpFileSystemConnector : IPoolableAdapter
{
    #region Instances

    private readonly ILogger _logger;
    private ISshCertificateAuthorityService _sshCaService;

    #endregion

    #region Constructors

    public SftpFileSystemConnector(ILogger logger, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _logger = logger;
        _sshCaService = sshCertificateAuthorityService;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Create SFTP connection object
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="credentials">Credentials</param>
    /// <param name="proxy">Proxy</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    public object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials credentials,
        ClusterProxyConnection proxy, string sshCaToken, int? port)
    {
        var sftpClient = (SftpClient)(credentials.AuthenticationType switch
        {
            ClusterAuthenticationCredentialsAuthType.Password
                => CreateConnectionObjectUsingPasswordAuthentication(masterNodeName, credentials.Username,
                    credentials.Password, port),

            ClusterAuthenticationCredentialsAuthType.PasswordInteractive
                => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(masterNodeName,
                    credentials.Username, credentials.Password),

            ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey
                => CreateConnectionObjectUsingPrivateKeyAndPasswordAuthentication(masterNodeName, credentials.Username,
                    credentials.Password, credentials.PrivateKey, credentials.PrivateKeyPassphrase, port),

            ClusterAuthenticationCredentialsAuthType.PrivateKey
                => CreateConnectionObjectUsingPrivateKeyAuthentication(masterNodeName, credentials.Username,
                    credentials.PrivateKey, credentials.PrivateKeyPassphrase, port),

            ClusterAuthenticationCredentialsAuthType.PasswordViaProxy
                => CreateConnectionObjectUsingPasswordAuthenticationViaProxy(proxy.Host, proxy.Type, proxy.Port,
                    proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.Password, port),

            ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy
                => CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractiveViaProxy(proxy.Host,
                    proxy.Type, proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username,
                    credentials.Password, port),

            ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy
                => CreateConnectionObjectUsingPrivateKeyAndPasswordAuthenticationViaProxy(proxy.Host, proxy.Type,
                    proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials.Username,
                    credentials.Password, credentials.PrivateKey, credentials.PrivateKeyPassphrase, port),

            ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy
                => CreateConnectionObjectUsingPrivateKeyAuthenticationViaProxy(proxy.Host, proxy.Type, proxy.Port,
                    proxy.Username, proxy.Password, masterNodeName, credentials.Username, credentials.PrivateKey,
                    credentials.PrivateKeyPassphrase, port),

            ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, credentials.Username, port),

            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, credentials.Username, port),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificate => 
                CreateConnectionObjectUsingSshCertificate(masterNodeName, credentials, sshCaToken, port),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy => 
                CreateConnectionObjectUsingSshCertificateViaProxy(proxy.Host, proxy.Type,
                    proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials, sshCaToken, port),

            _ => throw new NotImplementedException(
                "SFTP authentication credentials authentication type is not allowed!")
        });
        sftpClient.ConnectionInfo.RetryAttempts = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts;
        sftpClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout);
        return sftpClient;
    }

    private SftpClient CreateConnectionObjectUsingSshCertificateViaProxy(string proxyHost,
        ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName,
        ClusterAuthenticationCredentials credentials, string sshCaToken, int? port){
        try
        {
            string publicKey = credentials.PublicKey;
            if (string.IsNullOrEmpty(credentials.PublicKey))
            {
                publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(credentials).PublicKeyInAuthorizedKeysFormat;
            }
            var response = _sshCaService.SignAsync(publicKey, sshCaToken, masterNodeName)
                .GetAwaiter()
                .GetResult();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentials.PrivateKey));
            using var certificateStream = new MemoryStream(Encoding.UTF8.GetBytes(response.SshCert));
            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    string.IsNullOrEmpty(response.PosixUsername) ? credentials.Username : response.PosixUsername,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    string.IsNullOrEmpty(response.PosixUsername) ? credentials.Username : response.PosixUsername,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
            };

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
        
    }

    private SftpClient CreateConnectionObjectUsingSshCertificate(string masterNodeName, ClusterAuthenticationCredentials credentials, string sshCaToken, int? port)
    {
        try
        {
            string publicKey = credentials.PublicKey;
            if (string.IsNullOrEmpty(credentials.PublicKey))
            {
                publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(credentials).PublicKeyInAuthorizedKeysFormat;
            }
            var response = _sshCaService.SignAsync(publicKey, sshCaToken, masterNodeName)
                .GetAwaiter()
                .GetResult();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentials.PrivateKey));
            using var certificateStream = new MemoryStream(Encoding.UTF8.GetBytes(response.SshCert));
            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    string.IsNullOrEmpty(response.PosixUsername) ? credentials.Username : response.PosixUsername,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    string.IsNullOrEmpty(response.PosixUsername) ? credentials.Username : response.PosixUsername,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream))
            };

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw e;
        }
        
    }

    /// <summary>
    ///     Connect client to server
    /// </summary>
    /// <param name="connectorClient"></param>
    public void Connect(object connectorClient)
    {
        new SftpClientAdapter((SftpClient)connectorClient).Connect();
    }

    /// <summary>
    ///     Disconnect client from server
    /// </summary>
    /// <param name="connectorClient"></param>
    public void Disconnect(object connectorClient)
    {
        new SftpClientAdapter((SftpClient)connectorClient).Disconnect();
    }

    #endregion

    #region Local Methods

    /// <summary>
    ///     Create connection object using password authentication
    /// </summary>
    /// <param name="masterNodeName">Master host name</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    private static object CreateConnectionObjectUsingPasswordAuthentication(string masterNodeName, string username,
        string password, int? port)
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

        return new SftpClient(connectionInfo);
    }

    /// <summary>
    ///     Create connection object using password authentication via Proxy
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
    private static object CreateConnectionObjectUsingPasswordAuthenticationViaProxy(string proxyHost,
        ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName,
        string username, string password, int? port)
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
        return new SftpClient(connectionInfo);
    }

    /// <summary>
    ///     Create connection object using password authentication with keyboard interactive
    /// </summary>
    /// <param name="masterNodeName">Master host name</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns></returns>
    private static object CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractive(
        string masterNodeName, string username, string password)
    {
        var connectionInfo = new KeyboardInteractiveConnectionInfo(masterNodeName, username);
        connectionInfo.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
        {
            foreach (var prompt in e.Prompts) prompt.Response = password;
        };
        return new SftpClient(connectionInfo);
    }

    /// <summary>
    ///     Create connection object using password authentication with keyboard interactive via Proxy
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
    private static object CreateConnectionObjectUsingPasswordAuthenticationWithKeyboardInteractiveViaProxy(
        string proxyHost, ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword,
        string masterNodeName, string username, string password, int? port)
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

        connectionInfo.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
        {
            foreach (var prompt in e.Prompts) prompt.Response = password;
        };
        return new SftpClient(connectionInfo);
    }

    /// <summary>
    ///     Create connection object using private key authentication
    /// </summary>
    /// <param name="masterNodeName">Master host name</param>
    /// <param name="username">Username</param>
    /// <param name="privateKeyFile">Private key file</param>
    /// <param name="privateKeyPassword">Private key password</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    private static object CreateConnectionObjectUsingPrivateKeyAuthentication(string masterNodeName, string username,
        string privateKey, string privateKeyPassword, int? port)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    username,
                    new PrivateKeyFile(stream, privateKeyPassword)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    username,
                    new PrivateKeyFile(stream, privateKeyPassword))
            };

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SFTPCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    /// <summary>
    ///     Create connection object using private key authentication via Proxy
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
    private static object CreateConnectionObjectUsingPrivateKeyAuthenticationViaProxy(string proxyHost,
        ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName,
        string username, string privateKey, string privateKeyPassword, int? port)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    username,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, privateKeyPassword)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    username,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, privateKeyPassword))
            };

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SFTPCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    /// <summary>
    ///     Create connection object using private key and password authentication
    /// </summary>
    /// <param name="masterNodeName">Master host name</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="privateKeyFile">Private key file</param>
    /// <param name="privateKeyPassword">Private key password</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    private static object CreateConnectionObjectUsingPrivateKeyAndPasswordAuthentication(string masterNodeName,
        string username, string password, string privateKey, string privateKeyPassword, int? port)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
            var connectionInfo = port switch
            {
                null => new ConnectionInfo(
                    masterNodeName,
                    username,
                    new PasswordAuthenticationMethod(username, password),
                    new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(stream, privateKeyPassword))),
                _ => new ConnectionInfo(
                    masterNodeName,
                    port.Value,
                    username,
                    new PasswordAuthenticationMethod(username, password),
                    new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(stream, privateKeyPassword)))
            };

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SFTPCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    /// <summary>
    ///     Create connection object using private key and password authentication via Proxy
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
    private static object CreateConnectionObjectUsingPrivateKeyAndPasswordAuthenticationViaProxy(string proxyHost,
        ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName,
        string username, string password, string privateKey, string privateKeyPassword, int? port)
    {
        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
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
                new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(stream, privateKeyPassword)));

            var client = new SftpClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SFTPCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    private NoAuthenticationSftpClient CreateConnectionObjectUsingNoAuthentication(string masterNodeName,
        string username, int? port)
    {
        return new NoAuthenticationSftpClient(_logger, masterNodeName, username, port);
    }

    #endregion
}
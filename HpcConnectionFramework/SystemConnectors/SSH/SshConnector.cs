using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HEAppE.CertificateGenerator;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.CertificateGenerator.Generators.v2;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshCaAPI;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh connector
/// </summary>
public class SshConnector : IPoolableAdapter
{
    private ISshCertificateAuthorityService _sshCaService;
    public SshConnector(ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _sshCaService = sshCertificateAuthorityService;
    }
    #region Local Methods

    /// <summary>
    ///     Create ssh connection object
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="credentials">Credentials</param>
    /// <param name="proxy">Proxy</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    public object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials credentials,
        ClusterProxyConnection proxy, string sshCaToken, int? port)
    {
        SshClient sshClient = (SshClient)(credentials.AuthenticationType switch
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
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, port, credentials.Username),

            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, port, credentials.Username),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificate => 
                CreateConnectionObjectUsingSshCertificate(masterNodeName, credentials, sshCaToken, port),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy => 
                CreateConnectionObjectUsingSshCertificateViaProxy(proxy.Host, proxy.Type,
                    proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials, sshCaToken, port),

            _ => throw new SshClientArgumentException("AuthenticationTypeNotAllowed")
        });
        sshClient.ConnectionInfo.RetryAttempts = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts;
        sshClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout);
        return sshClient;
    }

    private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings =
        HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;

    /// <summary>
    ///     Connect client to server
    /// </summary>
    /// <param name="connectorClient"></param>
    public void Connect(object connectorClient)
    {
        new SshClientAdapter((SshClient)connectorClient).Connect();
    }

    /// <summary>
    ///     Disconnect client from server
    /// </summary>
    /// <param name="connectorClient"></param>
    public void Disconnect(object connectorClient)
    {
        new SshClientAdapter((SshClient)connectorClient).Disconnect();
    }
    
    /// <summary>
    /// Is connection connected
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public bool IsConnected(object connection)
    {
        if (connection is SshClient sshClient)
        {
            return sshClient.IsConnected;
        }
        return false;
    }

    #endregion

    #region Private Methods

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

        return new SshClient(connectionInfo);
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
        return new SshClient(connectionInfo);
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
        return new SshClient(connectionInfo);
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
        return new SshClient(connectionInfo);
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    private static MemoryStream DecryptPksc8PrivateKey(string masterNodeName, string privateKeyFile,
        string privateKeyPassword)
    {
        MemoryStream privateKeyMemoryStream = null;
        switch (CipherGeneratorConfiguration.Type)
        {
            case FileTransferCipherType.Unknown:
                throw new SshCommandException(
                    $"Unknown cipher type for the private key that is used for the connection to \"{masterNodeName}\"!");
            case FileTransferCipherType.RSA3072:
            case FileTransferCipherType.RSA4096:
            {
                var key = RSA.Create();
                var encryptedPksc8Pk = File.ReadAllText(privateKeyFile);
                key.ImportFromEncryptedPem(encryptedPksc8Pk, privateKeyPassword);
                var pk = key.ExportRSAPrivateKeyPem();
                privateKeyMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(pk));
            }
                break;
            case FileTransferCipherType.nistP256:
            case FileTransferCipherType.nistP521:
            {
                var key = ECDsa.Create();
                var encryptedPksc8Pk = File.ReadAllText(privateKeyFile);
                key.ImportFromEncryptedPem(encryptedPksc8Pk, privateKeyPassword);
                var pk = key.ExportECPrivateKeyPem();
                privateKeyMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(pk));
            }
                break;
            case FileTransferCipherType.Ed25519:
            {
                var encryptedPksc8Pk = File.ReadAllText(privateKeyFile);
                var keyPair = (AsymmetricCipherKeyPair)new PemReader(new StringReader(encryptedPksc8Pk), new PasswordFinder(privateKeyPassword)).ReadObject();
                var pk = (Ed25519PrivateKeyParameters)keyPair.Private;
                privateKeyMemoryStream = new MemoryStream(pk.GetEncoded());
            }
                break;
            default:
                throw new SshCommandException(
                    $"Unknown cipher type for the private key that is used for the connection to \"{masterNodeName}\"!");
        }

        return privateKeyMemoryStream;
    }
    
    private SshClient CreateConnectionObjectUsingSshCertificate(string masterNodeName, ClusterAuthenticationCredentials credentials, string sshCaToken, int? port)
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw e;
        }
        
    }
    
    private SshClient CreateConnectionObjectUsingSshCertificateViaProxy(string proxyHost,
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
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

            var client = new SshClient(connectionInfo);
            return client;
        }
        catch (Exception e)
        {
            throw new SshCommandException("NotCorrespondingPasswordForPrivateKey", e, masterNodeName);
        }
    }

    /// <summary>
    ///     Create connection object using private key stored in memory (ssh-agent)
    /// </summary>
    /// <param name="masterNodeName">Master host name</param>
    /// <param name="port"></param>
    /// <param name="username">Username</param>
    /// <returns></returns>
    private static object CreateConnectionObjectUsingNoAuthentication(string masterNodeName, int? port,
        string username)
    {
        var client = new NoAuthenticationSshClient(masterNodeName, port, username);
        return client;
    }

    #endregion
}
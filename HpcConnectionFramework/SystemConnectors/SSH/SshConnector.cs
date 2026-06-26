#pragma warning disable CA2200
using System;
using System.Collections.Generic;
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
using SshCaAPI.Configuration;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;
using HEAppE.Services.Expirio;
using HEAppE.Services.Expirio.Models;
using System.Threading.Tasks;
using HEAppE.Services.Expirio.Configuration;

using Microsoft.Extensions.Logging;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh connector
/// </summary>
public class SshConnector : IPoolableAdapter
{
    private static System.Threading.SemaphoreSlim _krbConfigSemaphore => KerberosConfigHelper._krbConfigSemaphore;
    private ISshCertificateAuthorityService _sshCaService;
    private IExpirioService _expirio;
    private ILogger _logger;
    public SshConnector(ISshCertificateAuthorityService sshCertificateAuthorityService, IExpirioService expirio, ILogger logger)
    {
        _sshCaService = sshCertificateAuthorityService;
        _logger = logger;
        _expirio = expirio;
    }
    #region Local Methods

    /// <summary>
    ///     Create ssh connection object
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="credentials">Credentials</param>
    /// <param name="cluster">Cluster</param>
    /// <param name="port">Port</param>
    /// <returns></returns>
    public async Task<object> CreateConnectionObjectAsync(string masterNodeName, ClusterAuthenticationCredentials credentials,
        Cluster cluster, string sshCaToken, string lexisToken, int? port)
    {
        ClusterProxyConnection proxy = cluster.ProxyConnection;
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
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, port, credentials.Username, _logger),

            ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent
                => CreateConnectionObjectUsingNoAuthentication(masterNodeName, port, credentials.Username, _logger),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificate => 
                await CreateConnectionObjectUsingSshCertificateAsync(masterNodeName, credentials, sshCaToken, port),
            
            ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy => 
                await CreateConnectionObjectUsingSshCertificateViaProxyAsync(proxy.Host, proxy.Type,
                    proxy.Port, proxy.Username, proxy.Password, masterNodeName, credentials, sshCaToken, port),
            
            ClusterAuthenticationCredentialsAuthType.Kerberos => 
                await CreateConnectionObjectUsingKerberosAsync(masterNodeName, credentials.Username, 
                    !string.IsNullOrEmpty(cluster.DomainName) ? cluster.DomainName : masterNodeName, 
                    lexisToken, cluster, cluster.Port ?? port),

            _ => throw new SshClientArgumentException("AuthenticationTypeNotAllowed")
        });
        //TODO: Kerberos client need to support these properties.
        sshClient.ConnectionInfo.RetryAttempts = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts;
        sshClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout);
        sshClient.KeepAliveInterval = TimeSpan.FromSeconds(30);
        return sshClient;
    }

    private static readonly ClusterConnectionPoolConfiguration _connectionPoolSettings =
        HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;



    public async Task ConnectAsync(object connectorClient)
    {
        var adapter = new SshClientAdapter((SshClient)connectorClient);
        await adapter.ConnectAsync();

        if (connectorClient is SshClient sshClient && 
            !(connectorClient is KerberosSshClient) && 
            !(connectorClient is NoAuthenticationSshClient))
        {
            string destPath = "/opt/heappe/confs/krb5.conf";
            if (Directory.Exists(destPath))
            {
                destPath = Path.Combine(destPath, "krb5.conf");
            }
            bool needsDownload = false;
            try
            {
                if (!File.Exists(destPath) || (DateTime.UtcNow - File.GetLastWriteTimeUtc(destPath)).TotalHours >= 1)
                {
                    needsDownload = true;
                }
            }
            catch
            {
                needsDownload = true;
            }

            if (needsDownload)
            {
                await _krbConfigSemaphore.WaitAsync();
                try
                {
                    // Double-checked locking pattern: check state again after acquiring lock
                    if (!File.Exists(destPath) || (DateTime.UtcNow - File.GetLastWriteTimeUtc(destPath)).TotalHours >= 1)
                    {
                        _logger.LogInformation("Automatically downloading krb5.conf from connected host...");
                        var commandResult = await adapter.RunCommandAsync("cat /etc/krb5.conf");
                        if (commandResult != null && !string.IsNullOrWhiteSpace(commandResult.Result) && commandResult.ExitStatus == 0)
                        {
                            var dir = Path.GetDirectoryName(destPath);
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            
                            string existingContent = "";
                            if (File.Exists(destPath))
                            {
                                try { existingContent = await File.ReadAllTextAsync(destPath); } catch { }
                            }
                            string mergedContent = Krb5ConfigMerger.Merge(existingContent, commandResult.Result);

                            string tempPath = destPath + ".tmp";
                            await File.WriteAllTextAsync(tempPath, mergedContent);
                            File.Move(tempPath, destPath, overwrite: true);
                            
                            _logger.LogInformation($"Successfully auto-downloaded and saved krb5.conf to {destPath}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to auto-download krb5.conf: exit code {commandResult?.ExitStatus}, error: {commandResult?.Error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while auto-downloading krb5.conf from host.");
                }
                finally
                {
                    _krbConfigSemaphore.Release();
                }
            }
        }
    }

    /// <summary>
    ///     Disconnect client from server
    /// </summary>
    /// <param name="connectorClient"></param>
    public async Task DisconnectAsync(object connectorClient)
    {
        await new SshClientAdapter((SshClient)connectorClient).DisconnectAsync();
    }
    
    /// <summary>
    /// Is connection connected
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public bool IsConnected(object connection)
    {//TODO: do this implementation
        if (connection is SshClient sshClient)
        {
            try
            {
                return sshClient.IsConnected; 
            }
            catch
            {
                return false;
            }
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

        var client = new SshClient(connectionInfo);
        client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
        return client;
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
        var client = new SshClient(connectionInfo);
        client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
        return client;
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
        var client = new SshClient(connectionInfo);
        client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
        return client;
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
        var client = new SshClient(connectionInfo);
        client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
        return client;
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
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
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
                throw new SshCommandException("UnknownCipherType", masterNodeName);
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
                throw new SshCommandException("UnknownCipherType", masterNodeName);
        }

        return privateKeyMemoryStream;
    }
    
    private async Task<SshClient> CreateConnectionObjectUsingSshCertificateAsync(string masterNodeName, ClusterAuthenticationCredentials credentials, string sshCaToken, int? port)
    {
        try
        {
            string publicKey = credentials.PublicKey;
            if (string.IsNullOrEmpty(credentials.PublicKey))
            {
                publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(credentials).PublicKeyInAuthorizedKeysFormat;
            }
            var response = await _sshCaService.SignAsync(publicKey, sshCaToken, masterNodeName, _logger);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentials.PrivateKey));
            using var certificateStream = new MemoryStream(Encoding.UTF8.GetBytes(response.SshCert));
            if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(response.PosixUsername))
            {
                credentials.Username = response.PosixUsername;
            }

            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    credentials.Username,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    credentials.Username,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream))
            };

            var client = new SshClient(connectionInfo);
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
            return client;
        }
        catch (Exception e)
        {
            throw e;
        }
        
    }
    
    private async Task<SshClient> CreateConnectionObjectUsingSshCertificateViaProxyAsync(string proxyHost,
        ProxyType proxyType, int proxyPort, string proxyUsername, string proxyPassword, string masterNodeName,
        ClusterAuthenticationCredentials credentials, string sshCaToken, int? port){
        try
        {
            string publicKey = credentials.PublicKey;
            if (string.IsNullOrEmpty(credentials.PublicKey))
            {
                publicKey = SSHGenerator.GetPublicKeyFromPrivateKey(credentials).PublicKeyInAuthorizedKeysFormat;
            }
            var response = await _sshCaService.SignAsync(publicKey, sshCaToken, masterNodeName, _logger);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentials.PrivateKey));
            using var certificateStream = new MemoryStream(Encoding.UTF8.GetBytes(response.SshCert));
            if (SshCaSettings.UsePosixAccountFromCertificate && !string.IsNullOrEmpty(response.PosixUsername))
            {
                credentials.Username = response.PosixUsername;
            }

            var connectionInfo = port switch
            {
                null => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    credentials.Username,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
                _ => new PrivateKeyConnectionInfo(
                    masterNodeName,
                    port.Value,
                    credentials.Username,
                    proxyType.Map(),
                    proxyHost,
                    proxyPort,
                    proxyUsername ?? string.Empty,
                    proxyPassword ?? string.Empty,
                    new PrivateKeyFile(stream, credentials.PrivateKeyPassphrase, certificateStream)),
            };

            var client = new SshClient(connectionInfo);
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
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
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
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
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
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
            client.HostKeyReceived += (sender, e) => { e.CanTrust = true; };
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
    private static object CreateConnectionObjectUsingNoAuthentication(string masterNodeName, int? port, string username, ILogger logger)
    {
        var client = new NoAuthenticationSshClient(masterNodeName, port, username, logger);
        return client;
    }

    private async Task<SshClient> CreateConnectionObjectUsingKerberosAsync(string masterNodeName, string username, string addressHost, string lexisToken, Cluster cluster, int? port)
    {
        await KerberosConfigHelper.BootstrapConfigIfNeededAsync(masterNodeName, cluster, _logger);

        if(Tmds.Ssh.KrbLibSim.HasTicket(username) == false)
        {
            byte[] krbtkt = await GetKerberosTicket(lexisToken);
            Tmds.Ssh.KrbLibSim.AddOrUpdateTicketCache(krbtkt);
        }

        string address = port.HasValue ? $"{addressHost}:{port.Value}" : addressHost;
        return new KerberosSshClient(masterNodeName, address, username);
    }

    private static string GetDomainFromHostname(string hostname)
    {
        if (string.IsNullOrEmpty(hostname)) return "local";
        int firstDot = hostname.IndexOf('.');
        if (firstDot > 0 && firstDot < hostname.Length - 1)
        {
            return hostname.Substring(firstDot + 1);
        }
        return hostname;
    }

    /// <summary>
    ///     Get the kerberos ticket for a user given the LEXIS token.
    /// </summary>
    /// <param name="lexisToken"></param>
    /// <returns></returns>
    private async Task<byte[]> GetKerberosTicket(string lexisToken)
    {
        KerberosExchangeRequest request = new() { ProviderName = ExpirioSettings.ProviderName };
        string ticket = await _expirio.ExchangeTokenForKerberosAsync(request, lexisToken, _logger);
        return Convert.FromBase64String(ticket);
    }

    #endregion
}

public static class Krb5ConfigMerger
{
    public static string Merge(string existingContent, string newContent)
    {
        if (string.IsNullOrWhiteSpace(existingContent)) return newContent;
        if (string.IsNullOrWhiteSpace(newContent)) return existingContent;

        var existingSections = ParseSections(existingContent);
        var newSections = ParseSections(newContent);

        // Merge [libdefaults]
        if (newSections.TryGetValue("libdefaults", out var newLibdefaults))
        {
            if (!existingSections.TryGetValue("libdefaults", out var existingLibdefaults))
            {
                existingSections["libdefaults"] = newLibdefaults;
            }
            else
            {
                var existingLines = ParseLines(existingLibdefaults);
                var newLines = ParseLines(newLibdefaults);
                foreach (var kvp in newLines)
                {
                    existingLines[kvp.Key] = kvp.Value;
                }
                existingSections["libdefaults"] = FormatLines(existingLines);
            }
        }

        // Merge [realms]
        if (newSections.TryGetValue("realms", out var newRealms))
        {
            if (!existingSections.TryGetValue("realms", out var existingRealms))
            {
                existingSections["realms"] = newRealms;
            }
            else
            {
                var existingBlocks = ParseBlocks(existingRealms);
                var newBlocks = ParseBlocks(newRealms);
                foreach (var kvp in newBlocks)
                {
                    existingBlocks[kvp.Key] = kvp.Value;
                }
                existingSections["realms"] = FormatBlocks(existingBlocks);
            }
        }

        // Merge [domain_realm]
        if (newSections.TryGetValue("domain_realm", out var newDomainRealm))
        {
            if (!existingSections.TryGetValue("domain_realm", out var existingDomainRealm))
            {
                existingSections["domain_realm"] = newDomainRealm;
            }
            else
            {
                var existingLines = ParseLines(existingDomainRealm);
                var newLines = ParseLines(newDomainRealm);
                foreach (var kvp in newLines)
                {
                    existingLines[kvp.Key] = kvp.Value;
                }
                existingSections["domain_realm"] = FormatLines(existingLines);
            }
        }

        // Keep other sections from both
        foreach (var section in newSections.Keys)
        {
            if (!string.Equals(section, "libdefaults", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(section, "realms", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(section, "domain_realm", StringComparison.OrdinalIgnoreCase))
            {
                existingSections[section] = newSections[section];
            }
        }

        StringBuilder sb = new StringBuilder();
        foreach (var kvp in existingSections)
        {
            sb.AppendLine($"[{kvp.Key}]");
            sb.Append(kvp.Value);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ParseSections(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string currentSectionName = null;
        StringBuilder currentSectionContent = new StringBuilder();

        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (currentSectionName != null)
                    {
                        result[currentSectionName] = currentSectionContent.ToString();
                    }
                    currentSectionName = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    currentSectionContent.Clear();
                }
                else
                {
                    if (currentSectionName != null)
                    {
                        currentSectionContent.AppendLine(line);
                    }
                }
            }
            if (currentSectionName != null)
            {
                result[currentSectionName] = currentSectionContent.ToString();
            }
        }
        return result;
    }

    private static Dictionary<string, string> ParseLines(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
                int eqIdx = trimmed.IndexOf('=');
                if (eqIdx > 0)
                {
                    var key = trimmed.Substring(0, eqIdx).Trim();
                    var val = trimmed.Substring(eqIdx + 1).Trim();
                    result[key] = val;
                }
            }
        }
        return result;
    }

    private static string FormatLines(Dictionary<string, string> lines)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in lines)
        {
            sb.AppendLine($"    {kvp.Key} = {kvp.Value}");
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ParseBlocks(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = new StringReader(content))
        {
            string line;
            string currentBlockName = null;
            StringBuilder currentBlockContent = new StringBuilder();
            int braceCount = 0;

            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (currentBlockName == null)
                {
                    int eqIdx = trimmed.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        currentBlockName = trimmed.Substring(0, eqIdx).Trim();
                        var remainder = trimmed.Substring(eqIdx + 1).Trim();
                        currentBlockContent.Clear();
                        currentBlockContent.AppendLine($"    {currentBlockName} = {remainder}");
                        if (remainder.Contains("{")) braceCount++;
                        if (remainder.Contains("}")) braceCount--;
                        if (braceCount == 0)
                        {
                            result[currentBlockName] = currentBlockContent.ToString();
                            currentBlockName = null;
                        }
                    }
                }
                else
                {
                    currentBlockContent.AppendLine(line);
                    if (trimmed.Contains("{")) braceCount++;
                    if (trimmed.Contains("}")) braceCount--;
                    if (braceCount <= 0)
                    {
                        result[currentBlockName] = currentBlockContent.ToString();
                        currentBlockName = null;
                        braceCount = 0;
                    }
                }
            }
        }
        return result;
    }

    private static string FormatBlocks(Dictionary<string, string> blocks)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in blocks)
        {
            sb.Append(kvp.Value);
        }
        return sb.ToString();
    }
}
#pragma warning disable CS1030
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework.NetworkShare;
using HEAppE.FileTransferFramework.Sftp;
using HEAppE.FileTransferFramework.FirecRest;
using HEAppE.HpcConnectionFramework.Configuration;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using HEAppE.Services.Expirio;
using HEAppE.Services.FirecRest;

namespace HEAppE.FileTransferFramework;

public abstract class FileSystemFactory
{
    #region Constructors

    static FileSystemFactory()
    {
        using var serviceScope = ServiceActivator.GetScope();
        var loggerFactory = (ILoggerFactory)serviceScope.ServiceProvider.GetService(typeof(ILoggerFactory));
        _logger = loggerFactory.CreateLogger("HEAppE.FileTransferFramework.FileSystemFactory");
        _expirio = (IExpirioService)serviceScope.ServiceProvider.GetService(typeof(IExpirioService));
        _httpClientFactory = (IHttpClientFactory)serviceScope.ServiceProvider.GetService(typeof(IHttpClientFactory));
        _tokenService = (IFirecRestTokenService)serviceScope.ServiceProvider.GetService(typeof(IFirecRestTokenService));
    }

    #endregion

    #region Instances

    protected static readonly ILogger _logger;
    protected static readonly IExpirioService _expirio;
    protected static readonly IHttpClientFactory _httpClientFactory;
    protected static readonly IFirecRestTokenService _tokenService;
    private readonly ConcurrentDictionary<long, IConnectionPool> _schedulerConnPoolSingletons = new();
    private static FileSystemFactory _windowsSharedFactorySingleton;
    private static FileSystemFactory _sftpFactorySingleton;
    private static FileSystemFactory _firecrestFactorySingleton;

    #endregion

    #region Abstract Methods

    public abstract IRexFileSystemManager CreateFileSystemManager(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger);

    internal abstract IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile,
        ClusterAuthenticationCredentials credentials);

    protected abstract IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger);

    #endregion

    #region Local Methods

    public static FileSystemFactory GetInstance(FileTransferProtocol type)
    {
        return type switch
        {
            FileTransferProtocol.NetworkShare => _windowsSharedFactorySingleton ??= new NetworkShareFileSystemFactory(),
            FileTransferProtocol ftp when
                ftp == FileTransferProtocol.SftpScp ||
                ftp == FileTransferProtocol.LocalSftpScp => _sftpFactorySingleton ??= new SftpFileSystemFactory(),
            FileTransferProtocol ftp when
                ftp == FileTransferProtocol.Http ||
                ftp == FileTransferProtocol.Https => _firecrestFactorySingleton ??= new FirecRestFileSystemFactory(),
            _ => throw new SftpClientArgumentException("FactoryManagerTypeNotExists", type)
        };
    }

    protected IConnectionPool GetSchedulerConnectionPool(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger)
    {
        if (_schedulerConnPoolSingletons.TryGetValue(configuration.Id, out var connection))
        {
            return connection;
        }

        lock (_schedulerConnPoolSingletons)
        {
            if (!_schedulerConnPoolSingletons.TryGetValue(configuration.Id, out connection))
            {
                var poolSettings = HPCConnectionFrameworkConfiguration.ClustersConnectionPoolSettings;
                
                connection = new ConnectionPool.ConnectionPool(configuration.Cluster.MasterNodeName,
                    configuration.Cluster.TimeZone,
                    0, // MinSize
                    poolSettings.MaxConnectionsPerUser,
                    poolSettings.MaxSessionsPerConnection,
                    poolSettings.ConnectionPoolCleaningInterval,
                    poolSettings.ConnectionPoolMaxUnusedInterval,
                    CreateFileSystemConnector(configuration, sshCertificateAuthorityService, logger),
                    HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts,
                    HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout,
                    configuration.Cluster.Port,
                    logger);

                _schedulerConnPoolSingletons.TryAdd(configuration.Id, connection);
            }
            return connection;
        }
    }

    #endregion
}
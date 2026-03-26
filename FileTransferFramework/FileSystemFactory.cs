#pragma warning disable CS1030
﻿using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework.NetworkShare;
using HEAppE.FileTransferFramework.Sftp;
using HEAppE.HpcConnectionFramework.Configuration;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using HEAppE.Services.Expirio;

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
    }

    #endregion

    #region Instances

    protected static readonly ILogger _logger;
    protected static readonly IExpirioService _expirio;
    private readonly Dictionary<FileTransferMethod, IConnectionPool> _schedulerConnPoolSingletons = new();
    private static FileSystemFactory _windowsSharedFactorySingleton;
    private static FileSystemFactory _sftpFactorySingleton;
    
    
    private static readonly int ConnectionPoolMinSize = 0;
    private static readonly int ConnectionPoolMaxSize = 10;
    private static readonly int ConnectionPoolCleaningInterval = 60;
    private static readonly int ConnectionPoolMaxUnusedInterval = 1800;

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
            _ => throw new SftpClientArgumentException("FactoryManagerTypeNotExists", type)
        };
    }

    protected IConnectionPool GetSchedulerConnectionPool(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger)
    {
        if (!_schedulerConnPoolSingletons.TryGetValue(configuration, out var connection))
        {
            connection = new ConnectionPool.ConnectionPool(configuration.Cluster.MasterNodeName,
                configuration.Cluster.TimeZone,
                ConnectionPoolMinSize,
                ConnectionPoolMaxSize,
                ConnectionPoolCleaningInterval,
                ConnectionPoolMaxUnusedInterval,
                CreateFileSystemConnector(configuration, sshCertificateAuthorityService, logger),
                HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts,
                HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout,
                configuration.Cluster.Port,
                logger);

            _schedulerConnPoolSingletons.Add(configuration, connection);
        }

        return connection;
    }

    #endregion
}
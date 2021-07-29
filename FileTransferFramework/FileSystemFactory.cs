using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.FileTransferFramework.NetworkShare;
using HEAppE.FileTransferFramework.Sftp;
using Microsoft.Extensions.Logging;

namespace HEAppE.FileTransferFramework
{
    public abstract class FileSystemFactory
    {
        #region Instances
        protected static readonly ILogger _logger;
        private readonly Dictionary<FileTransferMethod, IConnectionPool> _schedulerConnPoolSingletons = new Dictionary<FileTransferMethod, IConnectionPool>();
        private static FileSystemFactory _windowsSharedFactorySingleton;
        private static FileSystemFactory _sftpFactorySingleton;
#warning TODO add to settings
        private static readonly int ConnectionPoolMinSize = 0;
        private static readonly int ConnectionPoolMaxSize = 10;
        private static readonly int ConnectionPoolCleaningInterval = 60;
        private static readonly int ConnectionPoolMaxUnusedInterval = 1800;
        #endregion
        #region Constructors
        static FileSystemFactory()
        {
#warning TODO temp solution before DI
            using var serviceScope = ServiceActivator.GetScope();
            ILoggerFactory loggerFactory = (ILoggerFactory) serviceScope.ServiceProvider.GetService(typeof(ILoggerFactory));
            _logger = loggerFactory.CreateLogger("HEAppE.FileTransferFramework.FileSystemFactory");
        }
        #endregion
        #region Abstract Methods
        public abstract IRexFileSystemManager CreateFileSystemManager(FileTransferMethod configuration);
        internal abstract IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile, ClusterAuthenticationCredentials credentials);
        protected abstract IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration);
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
                _ => throw new ApplicationException("File system manager factory with type \"" + type + "\" does not exist."),
            };
        }

        protected IConnectionPool GetSchedulerConnectionPool(FileTransferMethod configuration)
        {
            if (!_schedulerConnPoolSingletons.TryGetValue(configuration, out IConnectionPool connection))
            {
                connection = new ConnectionPool.ConnectionPool(configuration.Cluster.MasterNodeName,
                                                               configuration.Cluster.TimeZone,
                                                               ConnectionPoolMinSize,
                                                               ConnectionPoolMaxSize,
                                                               ConnectionPoolCleaningInterval,
                                                               ConnectionPoolMaxUnusedInterval,
                                                               CreateFileSystemConnector(configuration),
                                                               configuration.Cluster.Port);

                _schedulerConnPoolSingletons.Add(configuration, connection);
            }
            return connection;
        }
        #endregion
    }
}
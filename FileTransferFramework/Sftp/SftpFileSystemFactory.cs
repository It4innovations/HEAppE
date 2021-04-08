using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.FileTransferFramework.Sftp
{
    public class SftpFileSystemFactory : FileSystemFactory
    {
        #region Instances
        protected readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new Dictionary<string, IPoolableAdapter>();
        protected Dictionary<string, IRexFileSystemManager> _managerSingletons = new Dictionary<string, IRexFileSystemManager>();
        #endregion
        #region Override Methods
        public override IRexFileSystemManager CreateFileSystemManager(FileTransferMethod configuration)
        {
            if(!_managerSingletons.TryGetValue(configuration.ServerHostname, out IRexFileSystemManager fileManager))
            {
                fileManager = new SftpFileSystemManager(_logger, configuration, this, GetSchedulerConnectionPool(configuration));
                _managerSingletons.Add(configuration.ServerHostname, fileManager);
            }
            return fileManager;
        }

        internal override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile, ClusterAuthenticationCredentials credentials)
        {
            return syncFile.NameSpecification switch
            {
                FileNameSpecification.FullName => new SftpFullNameSynchronizer(syncFile, credentials),
                _ => default,
            };
        }

        protected override IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration)
        {
            string hostname = configuration.ServerHostname;
            if(!_connectorSingletons.TryGetValue(hostname, out IPoolableAdapter systemConnector))
            {
                systemConnector = new SftpFileSystemConnector(_logger);
                _connectorSingletons.Add(hostname, systemConnector);
            }
            return systemConnector;
        }
        #endregion
    }
}
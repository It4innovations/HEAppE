using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using SshCaAPI;

namespace HEAppE.FileTransferFramework.Sftp;

public class SftpFileSystemFactory : FileSystemFactory
{
    #region Instances

    protected readonly ConcurrentDictionary<string, IPoolableAdapter> _connectorSingletons = new();
    protected readonly ConcurrentDictionary<string, IRexFileSystemManager> _managerSingletons = new();

    #endregion

    #region Override Methods

    public override IRexFileSystemManager CreateFileSystemManager(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger)
    {
        if (_managerSingletons.TryGetValue(configuration.ServerHostname, out var fileManager))
        {
            return fileManager;
        }

        lock (_managerSingletons)
        {
            if (!_managerSingletons.TryGetValue(configuration.ServerHostname, out fileManager))
            {
                fileManager = new SftpFileSystemManager(logger, configuration, this, GetSchedulerConnectionPool(configuration, sshCertificateAuthorityService, logger));
                _managerSingletons.TryAdd(configuration.ServerHostname, fileManager);
            }
            return fileManager;
        }
    }

    internal override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile,
        ClusterAuthenticationCredentials credentials)
    {
        return syncFile.NameSpecification switch
        {
            FileNameSpecification.FullName => new SftpFullNameSynchronizer(syncFile, credentials),
            _ => default
        };
    }

    protected override IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger)
    {
        var hostname = configuration.ServerHostname;
        if (_connectorSingletons.TryGetValue(hostname, out var systemConnector))
        {
            return systemConnector;
        }

        lock (_connectorSingletons)
        {
            if (!_connectorSingletons.TryGetValue(hostname, out systemConnector))
            {
                systemConnector = new SftpFileSystemConnector(logger, sshCertificateAuthorityService, _expirio);
                _connectorSingletons.TryAdd(hostname, systemConnector);
            }
            return systemConnector;
        }
    }

    #endregion
}
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using SshCaAPI;

namespace HEAppE.FileTransferFramework.FirecRest;

public class FirecRestFileSystemFactory : FileSystemFactory
{
    private readonly ConcurrentDictionary<string, IRexFileSystemManager> _managerSingletons = new();

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
                fileManager = new FirecRestFileSystemManager(logger, configuration, this, _httpClientFactory, _expirio, _tokenService);
                _managerSingletons.TryAdd(configuration.ServerHostname, fileManager);
            }
            return fileManager;
        }
    }

    public override void InvalidateConnectionPool(long fileTransferMethodId)
    {
        base.InvalidateConnectionPool(fileTransferMethodId);
        foreach (var kvp in _managerSingletons)
        {
            if (kvp.Value is FirecRestFileSystemManager fcMgr && fcMgr.FileTransferConfiguration?.Id == fileTransferMethodId)
            {
                _managerSingletons.TryRemove(kvp.Key, out _);
            }
        }
    }

    public override void InvalidateAllConnectionPools()
    {
        base.InvalidateAllConnectionPools();
        _managerSingletons.Clear();
    }

    internal override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile,
        ClusterAuthenticationCredentials credentials)
    {
        throw new System.NotSupportedException("Synchronizers not supported for FirecREST filesystem manager.");
    }

    protected override IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService, ILogger logger)
    {
        throw new System.NotSupportedException("Connectors not supported for FirecREST filesystem manager.");
    }
}

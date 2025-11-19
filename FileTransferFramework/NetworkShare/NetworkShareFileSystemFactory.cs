using System;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using SshCaAPI;

namespace HEAppE.FileTransferFramework.NetworkShare;

public class NetworkShareFileSystemFactory : FileSystemFactory
{
    #region Override Methods

    public override IRexFileSystemManager CreateFileSystemManager(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new NetworkShareFileSystemManager(_logger, configuration, this);
    }

    internal override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification syncFile,
        ClusterAuthenticationCredentials credentials)
    {
        return syncFile.NameSpecification switch
        {
            FileNameSpecification.FullName => new NetworkShareFullNameSynchronizer(syncFile),
            _ => default
        };
    }

    /// <summary>
    ///     File system connector is not necessary for the network shared file system because no connection pool is needed.
    /// </summary>
    protected override IPoolableAdapter CreateFileSystemConnector(FileTransferMethod configuration, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        throw new NotImplementedException();
    }

    #endregion
}
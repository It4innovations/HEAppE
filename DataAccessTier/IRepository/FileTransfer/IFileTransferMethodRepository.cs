using HEAppE.DomainObjects.FileTransfer;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer
{
    public interface IFileTransferMethodRepository : IRepository<FileTransferMethod>
    {
        IEnumerable<FileTransferMethod> GetByClusterId(long clusterId);
    }
}
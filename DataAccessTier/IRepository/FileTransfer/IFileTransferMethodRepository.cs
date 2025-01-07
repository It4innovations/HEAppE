using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer;

public interface IFileTransferMethodRepository : IRepository<FileTransferMethod>
{
    IEnumerable<FileTransferMethod> GetByClusterId(long clusterId);
}
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer;

public interface IFileTransferMethodRepository : IRepository<FileTransferMethod>
{
    IEnumerable<FileTransferMethod> GetByClusterId(long clusterId);
    Task<IEnumerable<FileTransferMethod>> GetByClusterIdAsync(long clusterId);
}
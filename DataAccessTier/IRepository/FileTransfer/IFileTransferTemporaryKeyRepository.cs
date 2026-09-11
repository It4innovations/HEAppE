using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer;

public interface IFileTransferTemporaryKeyRepository : IRepository<FileTransferTemporaryKey>
{
    IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKeyExpiredBefore(System.DateTime threshold);
    Task<IEnumerable<FileTransferTemporaryKey>> GetAllActiveTemporaryKeyExpiredBeforeAsync(System.DateTime threshold);
    bool ContainsActiveTemporaryKey(string publicKey);
    Task<bool> ContainsActiveTemporaryKeyAsync(string publicKey);
}
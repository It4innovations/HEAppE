using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer;

public interface IFileTransferTemporaryKeyRepository : IRepository<FileTransferTemporaryKey>
{
    IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKey();
    Task<IEnumerable<FileTransferTemporaryKey>> GetAllActiveTemporaryKeyAsync();
    bool ContainsActiveTemporaryKey(string publicKey);
    Task<bool> ContainsActiveTemporaryKeyAsync(string publicKey);
}
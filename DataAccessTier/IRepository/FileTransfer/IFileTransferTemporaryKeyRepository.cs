using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer;

public interface IFileTransferTemporaryKeyRepository : IRepository<FileTransferTemporaryKey>
{
    IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKey();
    bool ContainsActiveTemporaryKey(string publicKey);
}
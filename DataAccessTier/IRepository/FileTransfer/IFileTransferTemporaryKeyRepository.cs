using HEAppE.DomainObjects.FileTransfer;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer
{
    public interface IFileTransferTemporaryKeyRepository : IRepository<FileTransferTemporaryKey>
    {
        IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKey();
        bool ContainsActiveTemporaryKey(string publicKey);
    }
}

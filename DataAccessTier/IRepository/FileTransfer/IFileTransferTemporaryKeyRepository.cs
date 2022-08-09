using HEAppE.DomainObjects.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.IRepository.FileTransfer
{
    public interface IFileTransferTemporaryKeyRepository : IRepository<FileTransferTemporaryKey>
    {
        IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKey();
        bool ContainsActiveTemporaryKey(string publicKey);
    }
}

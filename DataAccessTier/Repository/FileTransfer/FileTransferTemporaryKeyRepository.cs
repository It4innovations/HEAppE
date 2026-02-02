using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.Repository.FileTransfer;

internal class FileTransferTemporaryKeyRepository : GenericRepository<FileTransferTemporaryKey>,
    IFileTransferTemporaryKeyRepository
{
    #region Constructors

    internal FileTransferTemporaryKeyRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKey()
    {
        return GetAll().Where(x=>!x.IsDeleted).ToList();
    }

    public bool ContainsActiveTemporaryKey(string publicKey)
    {
        return GetAll().Any(w => w.PublicKey == publicKey);
    }

    #endregion
}
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.FileTransfer
{
    internal class FileTransferTemporaryKeyRepository : GenericRepository<FileTransferTemporaryKey>, IFileTransferTemporaryKeyRepository
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
            return GetAll().Where(w => !w.IsDeleted)
                            .ToList();
        }

        public bool ContainsActiveTemporaryKey(string publicKey)
        {
            return GetAll().Any(w => !w.IsDeleted && w.PublicKey == publicKey);
        }
        #endregion
    }
}

using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.Repository.FileTransfer;

internal class FileTransferMethodRepository : GenericRepository<FileTransferMethod>, IFileTransferMethodRepository
{
    #region Constructors

    internal FileTransferMethodRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public IEnumerable<FileTransferMethod> GetByClusterId(long clusterId)
    {
        return GetAll().Where(w => w.ClusterId == clusterId)
            .ToList();
    }

    #endregion
}
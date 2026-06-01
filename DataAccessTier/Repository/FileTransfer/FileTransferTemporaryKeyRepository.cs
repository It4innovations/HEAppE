using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;
using Microsoft.EntityFrameworkCore;

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
        return _dbSet
            .Include(x => x.SubmittedJob)
                .ThenInclude(j => j.Specification)
                    .ThenInclude(s => s.Cluster)
            .Include(x => x.SubmittedJob)
                .ThenInclude(j => j.Specification)
                    .ThenInclude(s => s.ClusterUser)
                        .ThenInclude(cu => cu.ClusterProjectCredentials)
                            .ThenInclude(cpc => cpc.AdaptorUser)
            .Include(x => x.SubmittedJob)
                .ThenInclude(j => j.Specification)
                    .ThenInclude(s => s.Project)
            .Where(x => !x.IsDeleted)
            .ToList();
    }

    public bool ContainsActiveTemporaryKey(string publicKey)
    {
        return GetAll().Any(w => w.PublicKey == publicKey);
    }

    #endregion
}
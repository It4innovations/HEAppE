using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public IEnumerable<FileTransferTemporaryKey> GetAllActiveTemporaryKeyExpiredBefore(System.DateTime threshold)
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
                        .ThenInclude(p => p.ClusterProjects)
                            .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Where(x => !x.IsDeleted && x.AddedAt <= threshold)
            .ToList();
    }

    public async Task<IEnumerable<FileTransferTemporaryKey>> GetAllActiveTemporaryKeyExpiredBeforeAsync(System.DateTime threshold)
    {
        return await _dbSet
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
                        .ThenInclude(p => p.ClusterProjects)
                            .ThenInclude(cp => cp.ClusterProjectCredentials)
            .Where(x => !x.IsDeleted && x.AddedAt <= threshold)
            .ToListAsync();
    }

    public bool ContainsActiveTemporaryKey(string publicKey)
    {
        return _dbSet.Any(w => w.PublicKey == publicKey);
    }

    public async Task<bool> ContainsActiveTemporaryKeyAsync(string publicKey)
    {
        return await _dbSet.AnyAsync(w => w.PublicKey == publicKey);
    }

    #endregion
}
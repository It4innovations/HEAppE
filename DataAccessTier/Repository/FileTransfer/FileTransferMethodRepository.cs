using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;
using Microsoft.EntityFrameworkCore;

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
        return _dbSet
            .Include(w => w.Cluster)
            .Where(w => w.ClusterId == clusterId)
            .ToList();
    }

    public async Task<IEnumerable<FileTransferMethod>> GetByClusterIdAsync(long clusterId)
    {
        return await _dbSet
            .Include(w => w.Cluster)
            .Where(w => w.ClusterId == clusterId)
            .ToListAsync();
    }

    #endregion
}
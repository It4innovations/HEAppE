using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DataAccessTier.Repository.FileTransfer
{
    internal class FileSpecificationRepository : GenericRepository<FileSpecification>, IFileSpecificationRepository
    {
        #region Constructors
        internal FileSpecificationRepository(MiddlewareContext context)
                : base(context)
        {

        }
        #endregion
    }
}

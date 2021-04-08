using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DomainObjects.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

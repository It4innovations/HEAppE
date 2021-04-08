using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class PropertyChangeSpecificationRepository : GenericRepository<PropertyChangeSpecification>, IPropertyChangeSpecificationRepository
    {
        #region Constructors
        internal PropertyChangeSpecificationRepository(MiddlewareContext context)
                : base(context)
        {

        }
        #endregion
    }
}

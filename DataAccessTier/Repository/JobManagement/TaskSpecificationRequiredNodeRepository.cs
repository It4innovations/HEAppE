using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class TaskSpecificationRequiredNodeRepository : GenericRepository<TaskSpecificationRequiredNode>, ITaskSpecificationRequiredNodeRepository
    {
        #region Constructors
        internal TaskSpecificationRequiredNodeRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}

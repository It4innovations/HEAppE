using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class SubmittedTaskAllocationNodeInfoRepository : GenericRepository<SubmittedTaskAllocationNodeInfo>, ISubmittedTaskAllocationNodeInfoRepository
    {
        #region Constructors
        internal SubmittedTaskAllocationNodeInfoRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}

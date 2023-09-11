using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

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

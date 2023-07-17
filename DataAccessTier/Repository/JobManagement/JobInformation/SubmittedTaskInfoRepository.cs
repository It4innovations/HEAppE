using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation
{
    internal class SubmittedTaskInfoRepository : GenericRepository<SubmittedTaskInfo>, ISubmittedTaskInfoRepository
    {
        #region Constructors
        internal SubmittedTaskInfoRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public IEnumerable<SubmittedTaskInfo> GetAllUnFinished()
        {
            return GetAll().Where(w => w.State < TaskState.Finished && w.State > TaskState.Configuring)
                            .ToList();
        }

        public IEnumerable<SubmittedTaskInfo> GetAllFinished()
        {
            return GetAll().Where(w => w.State >= TaskState.Finished)
                            .ToList();
        }
        #endregion
    }
}
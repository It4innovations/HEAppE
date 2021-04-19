﻿using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;

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
        public IEnumerable<SubmittedTaskInfo> ListAllUnfinished()
        {
            return GetAll().Where(w => w.State < TaskState.Finished && w.State > TaskState.Configuring)
                         .ToList();
        }
        #endregion
    }
}
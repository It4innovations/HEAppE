﻿using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation
{
    internal class SubmittedJobInfoRepository : GenericRepository<SubmittedJobInfo>, ISubmittedJobInfoRepository
    {
        #region Constructors
        internal SubmittedJobInfoRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId)
        {
            return GetAll().Where(w => w.Submitter.Id == submitterId && w.State < JobState.Finished || w.State == JobState.WaitingForServiceAccount)
                            .ToList();
        }

        public IEnumerable<SubmittedJobInfo> GetAllUnfinished()
        {
            return GetAll().Where(w => w.Tasks.Any(we => we.State > TaskState.Configuring && we.State < TaskState.Finished))
                             .ToList();

            //return GetAll().Where(w => w.State < JobState.Finished && w.State > JobState.Configuring || w.State == JobState.WaitingForServiceAccount)
            //                 .ToList();
        }

        public IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId)
        {
            return GetAll().Where(w => w.Submitter.Id == submitterId)
                            .ToList();
        }

        public IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount()
        {
            return GetAll().Where(w => w.State == JobState.WaitingForServiceAccount)
                            .OrderBy(w => w.Id)
                            .ToList();
        }
        #endregion
    }
}
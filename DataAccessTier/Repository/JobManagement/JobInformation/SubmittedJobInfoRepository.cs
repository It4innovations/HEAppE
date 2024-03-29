﻿using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<SubmittedJobInfo> GetAllWithSubmittedTaskAdaptorUserAndProject()
        {
            return _dbSet.Include(i => i.Submitter)
                         .Include(i => i.Specification)
                            .ThenInclude(i => i.Cluster)
                         .Include(i => i.Project)
                            .ThenInclude(i => i.CommandTemplates)
                                .ThenInclude(i => i.TemplateParameters)
                         .Include(i => i.Tasks)
                            .ThenInclude(i => i.Specification)
                                .ThenInclude(i => i.CommandTemplate)
                         .Include(i => i.Tasks)
                            .ThenInclude(i => i.Specification)
                                .ThenInclude(i => i.ClusterNodeType)
                                    .ThenInclude(i => i.Cluster)
                         .ToList();
        }
        #endregion
    }
}
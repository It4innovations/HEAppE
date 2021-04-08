﻿using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class TaskParalizationSpecificationRepository : GenericRepository<TaskParalizationSpecification>, ITaskParalizationSpecificationRepository
    {
        #region Constructors
        internal TaskParalizationSpecificationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}

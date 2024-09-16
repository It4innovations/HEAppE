using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class SubProjectRepository : GenericRepository<SubProject>, ISubProjectRepository
    {
        #region Constructors
        internal SubProjectRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public SubProject GetByIdentifier(string accountingString, long projectId)
        {
            return _context.SubProjects.FirstOrDefault(p => p.Identifier == accountingString && p.ProjectId == projectId);
        }
        #endregion
    }
}

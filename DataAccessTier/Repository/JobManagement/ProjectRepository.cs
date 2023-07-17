using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.JobManagement
{
    internal class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        #region Constructors
        internal ProjectRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public IEnumerable<Project> GetAllActiveProjects()
        {
            return _context.Projects.Where(p => !p.IsDeleted && p.EndDate >= DateTime.UtcNow);
        }

        public Project GetByAccountingString(string accountingString)
        {
            return _context.Projects.FirstOrDefault(p => p.AccountingString == accountingString);
        }
        #endregion
    }
}

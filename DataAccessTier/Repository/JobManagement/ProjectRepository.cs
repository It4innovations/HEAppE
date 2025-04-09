using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

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
        return _dbSet.Where(p => p.EndDate >= DateTime.UtcNow)
            .Include(x => x.ProjectContacts)
            .ThenInclude(x => x.Contact)
            .ToList();
    }

    public Project GetByAccountingString(string accountingString)
    {
        return _context.Projects.FirstOrDefault(p => p.AccountingString == accountingString);
    }

    #endregion
}
﻿using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command;

internal class CommandTemplateRepository : GenericRepository<CommandTemplate>, ICommandTemplateRepository
{
    #region Constructors

    internal CommandTemplateRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    public IList<CommandTemplate> GetCommandTemplatesByProjectId(long projectId)
    {
        return _dbSet.Where(w => w.ProjectId == projectId || w.ProjectId == null)
            .Include(i => i.Project)
            .Include(i => i.ClusterNodeType)
            .Include(i => i.TemplateParameters)
            .ToList();
    }
}
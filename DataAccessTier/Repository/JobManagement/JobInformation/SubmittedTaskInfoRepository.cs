using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;

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

    public SubmittedTaskInfo GetByIdWithJobSpecification(long id)
    {
        var task = _dbSet
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandTemplate)
                    .ThenInclude(ct => ct.TemplateParameters)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandParameterValues)
                    .ThenInclude(cpv => cpv.TemplateParameter)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.DependsOn)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.EnvironmentVariables)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.RequiredNodes)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.TaskParalizationSpecifications)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.RequestedNodeGroups)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.ClusterNodeType)
                    .ThenInclude(cnt => cnt.ClusterNodeTypeAggregation)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
                        .ThenInclude(c => c.ProxyConnection)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Project)
            .AsSplitQuery()
            .FirstOrDefault(t => t.Id == id);

        if (task == null) return null;

        if (task.Specification?.JobSpecification?.Cluster != null)
        {
            _context.Entry(task.Specification.JobSpecification.Cluster)
                .Collection(c => c.ClusterProjects)
                .Load();
        }

        if (task.Project != null)
        {
            _context.Entry(task.Project)
                .Collection(p => p.ClusterProjects)
                .Load();
        }

        return task;
    }

    #endregion
}
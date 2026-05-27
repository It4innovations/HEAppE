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
        return _dbSet
            .Include(t => t.Project)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.CommandTemplate)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Cluster)
                        .ThenInclude(c => c.ClusterProjects)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.ClusterUser)
            .Include(t => t.Specification)
                .ThenInclude(ts => ts.JobSpecification)
                    .ThenInclude(js => js.Project)
            .FirstOrDefault(t => t.Id == id);
    }

    #endregion
}
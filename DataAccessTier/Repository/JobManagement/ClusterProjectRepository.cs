using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class ClusterProjectRepository : GenericRepository<ClusterProject>, IClusterProjectRepository
{
    #region Constructors

    internal ClusterProjectRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public ClusterProject GetClusterProjectForClusterAndProject(long clusterId, long projectId)
    {
        return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId && cp.ClusterId == clusterId)
            .FirstOrDefault();
    }

    public IQueryable<ClusterProject> GetAllClusterProjectsForProject(long projectId)
    {
        return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId);
    }

    public IQueryable<ClusterProjectCredentialCheckLog> GetAllClusterProjectCredentialsCheckLogForProject(long projectId, DateTime? timeFrom, DateTime? timeTo)
    {   
        if (timeFrom.HasValue && timeTo.HasValue)
            return _context.ClusterProjectCredentialsCheckLog.Where(cl =>
                cl.ClusterProjectCredential.ClusterProject.ProjectId == projectId &&
                cl.CheckTimestamp >= timeFrom &&
                cl.CheckTimestamp < timeTo);

        if (timeFrom.HasValue)
            return _context.ClusterProjectCredentialsCheckLog.Where(cl =>
                cl.ClusterProjectCredential.ClusterProject.ProjectId == projectId &&
                cl.CheckTimestamp >= timeFrom);

        if (timeTo.HasValue)
            return _context.ClusterProjectCredentialsCheckLog.Where(cl =>
                cl.ClusterProjectCredential.ClusterProject.ProjectId == projectId &&
                cl.CheckTimestamp < timeTo);

        return _context.ClusterProjectCredentialsCheckLog.Where(cl =>
            cl.ClusterProjectCredential.ClusterProject.ProjectId == projectId
        );
    }

    public void AddClusterProjectCredentialCheckLog(ClusterProjectCredentialCheckLog checkLog)
    {
        _context.ClusterProjectCredentialsCheckLog.Add(checkLog);
    }

    public IQueryable<ClusterProjectCredential> GetAllClusterProjectCredentialsOrderByProjectAndThenByCluster()
    {
        return _context.ClusterProjectCredentials.AsQueryable();
    }

    #endregion
}

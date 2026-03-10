using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;


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
    
    public ClusterProject GetClusterProjectForClusterAndProjectIncludingDeleted(long clusterId, long projectId)
    {
        return _context.ClusterProjects
            .IgnoreQueryFilters() 
            .Include(x => x.ClusterProjectCredentials) // <--- TOTO CHYBĚLO
            .FirstOrDefault(x => x.ClusterId == clusterId && x.ProjectId == projectId);
    }
    public List<ClusterProject> GetClusterProjectForProject(long projectId)
    {
        return _context.ClusterProjects.Where(cp => cp.ProjectId == projectId)
            .ToList();
    }
    
    public List<ClusterProject> GetClusterProjectForProjectIncludeDeleted(long projectId)
    {
        return _context.ClusterProjects.IgnoreQueryFilters().Where(cp => cp.ProjectId == projectId)
            .ToList();
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

    public List<ClusterProjectCredential> GetAllActiveClusterProjectCredentialsUntracked()
    {
        var result = _context.ClusterProjectCredentials
            .Include(cpc => cpc.ClusterProject)
            .Include(cpc => cpc.ClusterProject.Cluster)
            .Include(cpc => cpc.ClusterProject.Cluster.NodeTypes)
            .Include(cpc => cpc.ClusterProject.Project)
            .Where(cpc => cpc.ClusterProject.Project.EndDate > DateTime.UtcNow &&
                          cpc.ClusterProject.Project.StartDate <= DateTime.UtcNow)
            .Include(cpc => cpc.ClusterAuthenticationCredentials)
            .AsNoTracking()
            .ToList();
        return result;
    }


    #endregion
}

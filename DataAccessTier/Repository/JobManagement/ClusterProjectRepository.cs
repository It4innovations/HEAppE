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

    private void Junk01(long projectId)
    {
        var x1 = _context.Projects.Where(p => p.Id == projectId);
        var x2 = x1.First().ClusterProjects;
        foreach (var x3 in x2)
            foreach (var x4 in x3.ClusterProjectCredentials)
                foreach (var x5 in x4.ClusterProjectCredentialsCheckLog)
                    ;
    }

    private void Junk02()
    {
        var rnd = new Random();
        var checkTimestamp = DateTime.Today;
        for (var i = 0; i < 100; i++)
        {
            _context.ClusterProjectCredentialsCheckLog.Add(new ClusterProjectCredentialCheckLog()
            {
                ClusterProjectId = 1,
                ClusterAuthenticationCredentialsId = 1,
                CheckTimestamp = checkTimestamp,
                VaultCredentialOk = rnd.NextDouble() < 0.9,
                ClusterConnectionOk = rnd.NextDouble() < 0.8,
                DryRunJobOk = rnd.NextDouble() < 0.7,
                ErrorMessage = "Lorem ipsum dolor sit amet",
                CreatedAt = checkTimestamp
            });
            checkTimestamp = checkTimestamp.AddSeconds(15);
        }
    }

    public IQueryable<ClusterProjectCredential> GetAllClusterProjectCredentialsOrderByProjectAndThenByCluster()
    {
        try
        {
            //FormattableString sql = $"""SELECT TOP(100) PERCENT CPC.* FROM ClusterProjectCredentials CPC, C""";
            //var tmp = _context.ClusterProjectCredentials.FromSql(sql).ToList();
            return _context.ClusterProjectCredentials.AsQueryable();
        } catch(Exception e) {
            e = e;
            throw e;
        }
    }

    public void DoSomething()
    {
        //Junk02();
    }

    #endregion
}

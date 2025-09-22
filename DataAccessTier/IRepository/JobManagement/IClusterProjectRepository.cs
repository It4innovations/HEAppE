using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterProjectRepository : IRepository<ClusterProject>
{
    ClusterProject GetClusterProjectForClusterAndProject(long clusterId, long projectId);

    IQueryable<ClusterProject> GetAllClusterProjectsForProject(long projectId);

    public IQueryable<ClusterProjectCredentialCheckLog> GetAllClusterProjectCredentialsCheckLogForProject(long projectId, DateTime? timeFrom, DateTime? timeTo);

    void DoSomething();

    public IQueryable<ClusterProjectCredential> GetAllClusterProjectCredentialsOrderByProjectAndThenByCluster();
}
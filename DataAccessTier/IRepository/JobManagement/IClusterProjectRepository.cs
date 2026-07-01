using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement;

public interface IClusterProjectRepository : IRepository<ClusterProject>
{
    ClusterProject GetClusterProjectForClusterAndProject(long clusterId, long projectId);
    Task<ClusterProject> GetClusterProjectForClusterAndProjectAsync(long clusterId, long projectId);

    ClusterProject GetClusterProjectForClusterAndProjectIncludingDeleted(long clusterId, long projectId);
    Task<ClusterProject> GetClusterProjectForClusterAndProjectIncludingDeletedAsync(long clusterId, long projectId);
    public List<ClusterProject> GetClusterProjectForProjectIncludeDeleted(long projectId);
    Task<List<ClusterProject>> GetClusterProjectForProjectIncludeDeletedAsync(long projectId);
    
    public List<ClusterProject> GetClusterProjectForProject(long projectId);
    Task<List<ClusterProject>> GetClusterProjectForProjectAsync(long projectId);
    
    IQueryable<ClusterProject> GetAllClusterProjectsForProject(long projectId);
    IQueryable<ClusterProject> AsQueryable();

    public IQueryable<ClusterProjectCredentialCheckLog> GetAllClusterProjectCredentialsCheckLogForProject(long projectId, DateTime? timeFrom, DateTime? timeTo);

    public void AddClusterProjectCredentialCheckLog(ClusterProjectCredentialCheckLog checkLog);

    public List<ClusterProjectCredential> GetAllActiveClusterProjectCredentialsUntracked();
    Task<List<ClusterProjectCredential>> GetAllActiveClusterProjectCredentialsUntrackedAsync();
}
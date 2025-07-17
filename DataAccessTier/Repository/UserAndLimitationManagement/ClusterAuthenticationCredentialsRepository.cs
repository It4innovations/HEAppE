using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using log4net;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class ClusterAuthenticationCredentialsRepository : GenericRepository<ClusterAuthenticationCredentials>,
    IClusterAuthenticationCredentialsRepository
{
    private static readonly ILog _log = LogManager.GetLogger(typeof(ClusterAuthenticationCredentialsRepository));
    private readonly IVaultConnector _vaultConnector;

    #region Constructors

    internal ClusterAuthenticationCredentialsRepository(MiddlewareContext context, IVaultConnector vaultConnector)
        : base(context)
    {
        _vaultConnector = vaultConnector;
    }

    #endregion


    private IEnumerable<ClusterAuthenticationCredentials> WithVaultData(
        IEnumerable<ClusterAuthenticationCredentials> credentials)
    {
        if (credentials == null) return Enumerable.Empty<ClusterAuthenticationCredentials>();

        foreach (var item in credentials)
        {
            if(item == null) continue;
            _log.Debug($"Importing VaultInfo for id:{item.Id}");
            var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(item.Id).GetAwaiter().GetResult();
            item.ImportVaultData(vaultData);
        }

        return credentials;
    }

    private ClusterAuthenticationCredentials WithVaultData(ClusterAuthenticationCredentials credentials)
    {
        if (credentials != null)
        {
            var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(credentials.Id).GetAwaiter()
                .GetResult();
            credentials.ImportVaultData(vaultData);
        }

        return credentials;
    }

    #region Methods

    public override ClusterAuthenticationCredentials GetById(long id)
    {
        var dbEntity = base.GetById(id);
        var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(id).GetAwaiter().GetResult();
        dbEntity.ImportVaultData(vaultData);
        return dbEntity;
    }

    public override void Delete(ClusterAuthenticationCredentials entityToDelete)
    {
        _vaultConnector.DeleteClusterAuthenticationCredentials(entityToDelete.Id);
        base.Delete(entityToDelete);
    }

    public override void Delete(long id)
    {
        _vaultConnector.DeleteClusterAuthenticationCredentials(id);
        base.Delete(id);
    }

    public override void Insert(ClusterAuthenticationCredentials entity)
    {
        base.Insert(entity);
    }

    public override void Update(ClusterAuthenticationCredentials entityToUpdate)
    {
        base.Update(entityToUpdate);
        _vaultConnector.SetClusterAuthenticationCredentials(entityToUpdate.ExportVaultData());
    }

    public override IList<ClusterAuthenticationCredentials> GetAll()
    {
        return WithVaultData(base.GetAll()).ToList();
    }

    public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForClusterAndProject(
        long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterProject =
            _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
        
        var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized));
        var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials).ToList();
        return WithVaultData(credentials);
    }

    public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsForUsernameAndProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac =>
            cac.Username == username &&
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();
        return WithVaultData(credentials).ToList();
    }

    public IEnumerable<ClusterAuthenticationCredentials> GetAuthenticationCredentialsProject(long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac =>
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();

        return WithVaultData(credentials);
    }

    public ClusterAuthenticationCredentials GetServiceAccountCredentials(long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterProject =
            _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
        var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => cpc.IsServiceAccount && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized));
        var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials);
        var cred = credentials?.FirstOrDefault();
        return WithVaultData(cred);
    }

    public IEnumerable<ClusterAuthenticationCredentials> GetAllGeneratedWithFingerprint(string fingerprint,
        long projectId)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.IsGenerated && x.PublicKeyFingerprint == fingerprint &&
                        x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return WithVaultData(credentials);
    }

    public IEnumerable<ClusterAuthenticationCredentials> GetAllGenerated(long projectId)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.IsGenerated && x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return WithVaultData(credentials);
    }

    #endregion
}
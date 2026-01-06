using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Exceptions.External;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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


    private async Task<IEnumerable<ClusterAuthenticationCredentials>> WithVaultData(
        IEnumerable<ClusterAuthenticationCredentials> credentials)
    {
        if (credentials == null) return Enumerable.Empty<ClusterAuthenticationCredentials>();

        var tasks = credentials
            .Where(c => c != null)
            .Select(async item =>
            {
                _log.Debug($"Importing VaultInfo for id:{item.Id}");
                var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(item.Id);
                item.ImportVaultData(vaultData);
                return item;
            });

        return await Task.WhenAll(tasks);
    }

    private async Task<ClusterAuthenticationCredentials> WithVaultData(ClusterAuthenticationCredentials credentials)
    {
        if (credentials != null)
        {
            var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(credentials.Id);
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

    public async Task DeleteAsync(ClusterAuthenticationCredentials entityToDelete)
    {
        await _vaultConnector.DeleteClusterAuthenticationCredentialsAsync(entityToDelete.Id);
        await base.DeleteAsync(entityToDelete);
    }

    public async Task DeleteAsync(long id)
    {
        await _vaultConnector.DeleteClusterAuthenticationCredentialsAsync(id);
        await base.DeleteAsync(id);
    }

    public override void Insert(ClusterAuthenticationCredentials entity)
    {
        base.Insert(entity);
    }

    public async Task UpdateAsync(ClusterAuthenticationCredentials entityToUpdate)
    {
        await base.UpdateAsync(entityToUpdate);
        await _vaultConnector.SetClusterAuthenticationCredentialsAsync(entityToUpdate.ExportVaultData());
    }

    public override async Task<IList<ClusterAuthenticationCredentials>> GetAllAsync()
    {
        var credentials = base.GetAll();
        var result = await WithVaultData(credentials);
        return result.ToList();
    }


    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForClusterAndProject(
        long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterProject =
            _context.ClusterProjects.FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
        
        var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized));
        var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials).ToList();
        if(requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            _log.Info($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
            
        }
        return await WithVaultData(credentials);
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForUsernameAndProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac =>
            cac.Username == username &&
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();
        return (await WithVaultData(credentials)).ToList();
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(long projectId,
        bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac =>
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();
        if(requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            _log.Info($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return (await WithVaultData(credentials));
    }
    
    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac => cac.Username == username &&
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();
        if(requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            _log.Info($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return (await WithVaultData(credentials));
    }

    public async Task<ClusterAuthenticationCredentials> GetServiceAccountCredentials(
        long clusterId,
        long projectId,
        bool requireIsInitialized,
        long? adaptorUserId)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.IsOneToOneMapping })
            .SingleOrDefaultAsync();

        if (project == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        var cred = await _context.ClusterProjects
            .AsNoTracking()
            .Where(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId)
            .SelectMany(cp => cp.ClusterProjectCredentials)
            .Where(cpc =>
                cpc.IsServiceAccount &&
                (!requireIsInitialized || cpc.IsInitialized) &&
                (project.IsOneToOneMapping
                    ? cpc.AdaptorUserId == adaptorUserId
                    : cpc.AdaptorUserId == null))
            .Select(cpc => cpc.ClusterAuthenticationCredentials)
            .FirstOrDefaultAsync();

        if (requireIsInitialized && cred == null)
        {
            _log.Info(
                $"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return await WithVaultData(cred);
    }


    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGeneratedWithFingerprint(string fingerprint,
        long projectId)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.PublicKeyFingerprint == fingerprint &&
                        x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return (await WithVaultData(credentials));
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGenerated(long projectId)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return (await WithVaultData(credentials));
    }

    #endregion
}
#pragma warning disable CS0108
﻿using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Exceptions.External;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class ClusterAuthenticationCredentialsRepository : GenericRepository<ClusterAuthenticationCredentials>,
    IClusterAuthenticationCredentialsRepository
{
    private readonly IVaultConnector _vaultConnector;
    private readonly ILogger _logger;

    #region Constructors

    internal ClusterAuthenticationCredentialsRepository(MiddlewareContext context, IVaultConnector vaultConnector, ILogger logger)
        : base(context)
    {
        _vaultConnector = vaultConnector;
        _logger = logger;
    }

    public async Task<IEnumerable<ClusterProjectCredential>> GetClusterProjectCredentials(long projectId, long? adaptorUserId, bool isAdministrator = false)
    {
        var project = _context.Projects.Find(projectId);
        if (project is null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        var isOneToOneMapping = project.IsOneToOneMapping;

        var query = _context.ClusterProjectCredentials
            .Include(cpc => cpc.ClusterAuthenticationCredentials)
            .Include(cpc => cpc.ClusterProject)
            .Where(cpc => cpc.ClusterProject.ProjectId == projectId && !cpc.IsDeleted);

        if (!(isAdministrator && adaptorUserId == null))
        {
            if (isOneToOneMapping)
            {
                query = query.Where(cpc => cpc.AdaptorUserId == adaptorUserId);
            }
            else
            {
                query = query.Where(cpc => cpc.AdaptorUserId == null);
            }
        }

        var results = await query.ToListAsync();

        // Load vault data for the associated credentials
        var credentials = results.Select(cpc => cpc.ClusterAuthenticationCredentials).Where(c => c != null).Distinct().ToList();
        if (credentials.Any())
        {
            await WithVaultData(credentials, _logger);
        }

        return results;
    }

    #endregion

    
    private async Task<IEnumerable<ClusterAuthenticationCredentials>> WithVaultData(
        IEnumerable<ClusterAuthenticationCredentials> credentials, ILogger logger)
    {
        if (credentials == null) return Enumerable.Empty<ClusterAuthenticationCredentials>();

        var activeLogger = logger ?? _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var result = new List<ClusterAuthenticationCredentials>();
        foreach (var item in credentials.Where(c => c != null))
        {
            activeLogger.LogDebug($"Importing VaultInfo for id:{item.Id}");
            var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(item.Id);
            item.ImportVaultData(vaultData);
            result.Add(item);
        }

        return result;
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

    public override async Task<ClusterAuthenticationCredentials> GetByIdAsync(long id)
    {
        var dbEntity = await base.GetByIdAsync(id);
        if (dbEntity == null) return null;
        var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(id);
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
        var credentials = _dbSet.Include(x=>x.ClusterProjectCredentials).ToList();
        var result = await WithVaultData(credentials, _logger);
        return result.ToList();
    }


    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForClusterAndProject(
        long clusterId, long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger = null)
    {
        var activeLogger = logger ?? _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterProject =
            _context.ClusterProjects
                .Include(cp => cp.ClusterProjectCredentials)
                    .ThenInclude(cpc => cpc.ClusterAuthenticationCredentials)
                .FirstOrDefault(cp => cp.ClusterId == clusterId && cp.ProjectId == projectId);
        
        var clusterProjectCredentials = clusterProject?.ClusterProjectCredentials.FindAll(cpc => !cpc.IsServiceAccount && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized));
        var credentials = clusterProjectCredentials?.Select(c => c.ClusterAuthenticationCredentials).ToList();
        if(requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            activeLogger.LogInformation($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
            
        }
        return await WithVaultData(credentials, logger);
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsForUsernameAndProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger = null)
    {
        var isOneToOneMapping = _context.Projects.Find(projectId).IsOneToOneMapping;
        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials.Where(cac =>
            cac.Username == username &&
            cac.ClusterProjectCredentials.Any(cpc => cpc.ClusterProject.ProjectId == projectId && (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null) && (!requireIsInitialized || cpc.IsInitialized)));
        var credentials = clusterAuthenticationCredentials?.Select(x => x).ToList();
        return (await WithVaultData(credentials, logger)).ToList();
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(long projectId,
        bool requireIsInitialized, long? adaptorUserId, ILogger logger = null, bool isAdministrator = false)
    {
        var activeLogger = logger ?? _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var project = _context.Projects.Find(projectId);
        if (project is null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);
        
        var isOneToOneMapping = project.IsOneToOneMapping;

        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Cluster)
                        .ThenInclude(c => c.NodeTypes)
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Cluster)
                        .ThenInclude(c => c.FileTransferMethods)
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Project)
            .Where(cac =>
            cac.ClusterProjectCredentials.Any(cpc => 
                cpc.ClusterProject.ProjectId == projectId && 
                (
                    (isAdministrator && adaptorUserId == null) || 
                    (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null)
                ) && 
                (!requireIsInitialized || cpc.IsInitialized)
            )
        );

        var credentials = clusterAuthenticationCredentials?.ToList();

        //return ids of credentials for logging
        var credentialIds = credentials?.Select(c => c.Id).ToList();
        _logger.LogDebug($"Found credentials for project {projectId} with adaptorUserId {adaptorUserId}: {string.Join(", ", credentialIds ?? new List<long>())}");
        
        if (requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            activeLogger.LogInformation($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return (await WithVaultData(credentials, logger));
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAuthenticationCredentialsProject(
        string username, long projectId, bool requireIsInitialized, long? adaptorUserId, ILogger logger = null, bool isAdministrator = false)
    {
        var activeLogger = logger ?? _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var project = _context.Projects.Find(projectId);
        if (project is null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        var isOneToOneMapping = project.IsOneToOneMapping;

        var clusterAuthenticationCredentials = _context.ClusterAuthenticationCredentials
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Cluster)
                        .ThenInclude(c => c.NodeTypes)
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Cluster)
                        .ThenInclude(c => c.FileTransferMethods)
            .Include(cac => cac.ClusterProjectCredentials)
                .ThenInclude(cpc => cpc.ClusterProject)
                    .ThenInclude(cp => cp.Project)
            .Where(cac => 
            cac.Username == username &&
            cac.ClusterProjectCredentials.Any(cpc => 
                cpc.ClusterProject.ProjectId == projectId && 
                (
                    (isAdministrator && adaptorUserId == null) || 
                    (isOneToOneMapping ? cpc.AdaptorUserId == adaptorUserId : cpc.AdaptorUserId == null)
                ) && 
                (!requireIsInitialized || cpc.IsInitialized)
            )
        );

        var credentials = clusterAuthenticationCredentials?.ToList();
        //return ids of credentials for logging
        var credentialIds = credentials?.Select(c => c.Id).ToList();
        _logger.LogDebug($"Found credentials for project {projectId} with adaptorUserId {adaptorUserId} and username {username}: {string.Join(", ", credentialIds ?? new List<long>())}");
        if (requireIsInitialized && (credentials == null || !credentials.Any()))
        {
            activeLogger.LogInformation($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}. Please ensure that the credentials are initialized by `heappe/Management/InitializeClusterScriptDirectory` using accessing them.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return await WithVaultData(credentials, logger);
    }
    
    public async Task<ClusterAuthenticationCredentials> GetServiceAccountCredentials(
        long clusterId,
        long projectId,
        bool requireIsInitialized,
        long? adaptorUserId,
        ILogger logger = null)
    {
        var activeLogger = logger ?? _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        // 1. Get mapping type (this is fast)
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.IsOneToOneMapping })
            .SingleOrDefaultAsync();

        if (project == null)
            throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

        // 2. Optimized Query: Start from the junction table where IDs are already present
        var query = _context.ClusterProjectCredentials
            .Where(cpc => cpc.ClusterProject.ClusterId == clusterId && 
                          cpc.ClusterProject.ProjectId == projectId &&
                          cpc.IsServiceAccount);

        // Filter by user mapping
        if (project.IsOneToOneMapping)
            query = query.Where(cpc => cpc.AdaptorUserId == adaptorUserId);
        else
            query = query.Where(cpc => cpc.AdaptorUserId == null);

        if (requireIsInitialized)
            query = query.Where(cpc => cpc.IsInitialized);

        // 3. Select only the necessary credentials object
        // This removes the need for all the .Include() overhead
        var cred = await query
            .Select(cpc => cpc.ClusterAuthenticationCredentials)
            .FirstOrDefaultAsync();

        if (requireIsInitialized && cred == null)
        {
            activeLogger.LogInformation($"No initialized credentials found for project {projectId} with adaptorUserId {adaptorUserId}.");
            throw new NotAllowedException("ClusterAccountNotInitialized", projectId);
        }

        return await WithVaultData(cred);
    }


    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGeneratedWithFingerprint(string fingerprint,
        long projectId, ILogger logger = null)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.PublicKeyFingerprint == fingerprint &&
                        x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return (await WithVaultData(credentials, logger));
    }

    public async Task<IEnumerable<ClusterAuthenticationCredentials>> GetAllGenerated(long projectId, ILogger logger = null)
    {
        var credentials = _context.ClusterAuthenticationCredentials
            .Where(x => x.ClusterProjectCredentials.Any(y => y.ClusterProject.ProjectId == projectId))
            .ToList();
        return (await WithVaultData(credentials, logger));
    }

    public async Task<IList<ClusterAuthenticationCredentials>> GetAllByUserNameAsync(string username, ILogger logger = null)
    {
        //with all
        var credentials = _dbSet
            .Include(c => c.ClusterProjectCredentials)
            .ThenInclude(cpc => cpc.ClusterProject)
            .Where(c => c.Username == username)
            .ToList();
        return (await WithVaultData(credentials, logger)).ToList();
    }

    public async Task<ClusterAuthenticationCredentials> GetAnyServiceAccountCredentialsForClusterAsync(long clusterId, ILogger logger = null)
    {
        var query = _context.ClusterProjectCredentials
            .Where(cpc => cpc.ClusterProject.ClusterId == clusterId && cpc.IsServiceAccount && !cpc.IsDeleted);

        var cred = await query
            .Select(cpc => cpc.ClusterAuthenticationCredentials)
            .FirstOrDefaultAsync();

        if (cred == null) return null;
        return await WithVaultData(cred);
    }

    #endregion
}
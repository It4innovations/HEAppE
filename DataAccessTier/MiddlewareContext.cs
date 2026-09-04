#pragma warning disable CS4014
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.OpenStack;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.Internal;
using HEAppE.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HEAppE.DataAccessTier;

public class MiddlewareContext : DbContext
{
    #region Constructors

    public MiddlewareContext() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<MiddlewareContext>.Instance)
    {
    }

    public MiddlewareContext(ILogger logger)
    {
        _logger = logger;
    }

    public static void InitializeDatabase(ILogger logger)
    {
        if (logger.GetType().Name.Contains("NullLogger") || Environment.GetCommandLineArgs().Any(a => a.Contains("ef"))) return;

        if (!string.IsNullOrEmpty(MiddlewareContextSettings.ConnectionString) && !_isMigrated)
            lock (_lockObject)
            {
                if (!_isMigrated)
                    try
                    {
                        var localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
                        if (localRunEnv != "LocalWindows")
                        {
                            using (var context = new MiddlewareContext(logger))
                            {
                                if (!context.Database.CanConnect())
                                {
                                    logger.LogInformation("Starting migration and seeding into the new database.");
                                    context.Database.Migrate();
                                    try
                                    {
                                        var dbName = context.Database.GetDbConnection().Database;
                                        if (!string.IsNullOrEmpty(dbName))
                                        {
                                            var builder = new SqlCommandBuilder();
                                            var safeDbName = builder.QuoteIdentifier(dbName);
#pragma warning disable EF1002
                                            // nosemgrep: security_code_scan.SCS0002-1
                                            context.Database.ExecuteSqlRaw($"ALTER DATABASE {safeDbName} SET READ_COMMITTED_SNAPSHOT ON;");
#pragma warning restore EF1002
                                            logger.LogInformation($"RCSI isolation level has been successfully enabled for database: {dbName}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogWarning($"Could not automatically set RCSI on database creation: {ex.Message}");
                                    }
                                    context.EnsureDatabaseSeeded();
                                    _isMigrated = true;
                                }
                                else
                                {
                                    var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
                                    var definedMigrations = context.Database.GetMigrations().ToList();
                                    var lastApplied = appliedMigrations.LastOrDefault();
                                    var lastDefined = definedMigrations.LastOrDefault();
                                    var appliedCount = appliedMigrations.Count;
                                    var definedCount = definedMigrations.Count;

                                    logger.LogInformation($"Database status - Applied: {appliedCount} (last: {lastApplied}), Defined: {definedCount} (last: {lastDefined})");

                                    if (appliedCount == 0 || lastApplied != lastDefined || appliedCount != definedCount)
                                    {
                                        if (DatabaseMigrationSettings.AutoMigrateDatabase)
                                        {
                                            logger.LogInformation("Applying migrations to the database.");
                                            context.Database.Migrate();

                                            appliedMigrations = context.Database.GetAppliedMigrations().ToList();
                                            lastApplied = appliedMigrations.LastOrDefault();
                                            appliedCount = appliedMigrations.Count;

                                            if (appliedCount != definedCount || lastApplied != lastDefined)
                                            {
                                                var extraInDb = appliedMigrations.Except(definedMigrations).ToList();
                                                var missingInDb = definedMigrations.Except(appliedMigrations).ToList();
                                                logger.LogWarning($"Migration count still mismatching after migrate: {appliedCount} applied vs {definedCount} defined. Last in DB: {lastApplied}, Last in code: {lastDefined}");
                                                if (extraInDb.Any()) logger.LogWarning($"Extra in DB: {string.Join(", ", extraInDb)}");
                                                if (missingInDb.Any()) logger.LogWarning($"Missing in DB: {string.Join(", ", missingInDb)}");
                                            }

                                            _isMigrated = true;
                                        }
                                        else if (lastApplied != lastDefined || appliedCount < definedCount)
                                        {
                                            throw new DbContextException("MigrationMismatch");
                                        }
                                    }

                                    logger.LogInformation("Application and database migrations are compatible. Seeding data...");
                                    context.EnsureDatabaseSeeded();
                                    _isMigrated = true;
                                }
                            }
                        }
                        else
                        {
                            _isMigrated = true;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new DbContextException("MigrationError", ex);
                    }
            }
    }

    #endregion

    #region Instances

    private static readonly object _lockObject = new();
    private static volatile bool _isMigrated;
    private readonly ILogger _logger;

    #endregion

    #region Override Methods

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = MiddlewareContextSettings.ConnectionString;
        if (!string.IsNullOrEmpty(connectionString))
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!builder.MultipleActiveResultSets)
            {
                builder.MultipleActiveResultSets = true;
            }
            if (!builder.TrustServerCertificate)
            {
                builder.TrustServerCertificate = true;
            }
            connectionString = builder.ConnectionString;
        }

        optionsBuilder.UseSqlServer(connectionString ?? "Server=localhost;Database=dummy;MultipleActiveResultSets=True;TrustServerCertificate=true;",
            sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null));
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdaptorUserUserGroupRole>()
            .HasKey(ug => new { ug.AdaptorUserId, ug.AdaptorUserGroupId, ug.AdaptorUserRoleId });
        modelBuilder.Entity<AdaptorUserUserGroupRole>()
            .HasOne(ug => ug.AdaptorUser)
            .WithMany(u => u.AdaptorUserUserGroupRoles)
            .HasForeignKey(ug => new { ug.AdaptorUserId });
        modelBuilder.Entity<AdaptorUserUserGroupRole>()
            .HasOne(ug => ug.AdaptorUserGroup)
            .WithMany(g => g.AdaptorUserUserGroupRoles)
            .HasForeignKey(ug => new { ug.AdaptorUserGroupId });
        modelBuilder.Entity<AdaptorUserUserGroupRole>()
            .HasOne(ug => ug.AdaptorUserRole)
            .WithMany(g => g.AdaptorUserUserGroupRoles)
            .HasForeignKey(ug => new { ug.AdaptorUserRoleId });

        modelBuilder.Entity<AdaptorUserRole>().HasAlternateKey(x => x.Name);

        modelBuilder.Entity<OpenStackAuthenticationCredentialDomain>()
            .HasKey(ug => new { ug.OpenStackAuthenticationCredentialId, ug.OpenStackDomainId });
        modelBuilder.Entity<OpenStackAuthenticationCredentialDomain>()
            .HasOne(ug => ug.OpenStackDomain)
            .WithMany(u => u.OpenStackAuthenticationCredentialDomains)
            .HasForeignKey(ug => new { ug.OpenStackDomainId });
        modelBuilder.Entity<OpenStackAuthenticationCredentialDomain>()
            .HasOne(ug => ug.OpenStackAuthenticationCredential)
            .WithMany(g => g.OpenStackAuthenticationCredentialDomains)
            .HasForeignKey(ug => new { ug.OpenStackAuthenticationCredentialId });

        modelBuilder.Entity<OpenStackAuthenticationCredentialProject>()
            .HasKey(ug => new { ug.OpenStackAuthenticationCredentialId, ug.OpenStackProjectId });
        modelBuilder.Entity<OpenStackAuthenticationCredentialProject>()
            .HasOne(ug => ug.OpenStackProject)
            .WithMany(u => u.OpenStackAuthenticationCredentialProjects)
            .HasForeignKey(ug => new { ug.OpenStackProjectId });
        modelBuilder.Entity<OpenStackAuthenticationCredentialProject>()
            .HasOne(ug => ug.OpenStackAuthenticationCredential)
            .WithMany(g => g.OpenStackAuthenticationCredentialProjects)
            .HasForeignKey(ug => new { ug.OpenStackAuthenticationCredentialId });

        modelBuilder.Entity<TaskDependency>()
            .HasKey(td => new { td.TaskSpecificationId, td.ParentTaskSpecificationId });
        modelBuilder.Entity<TaskDependency>()
            .HasOne(td => td.ParentTaskSpecification)
            .WithMany(ts => ts.Depended)
            .HasForeignKey(td => new { td.ParentTaskSpecificationId })
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TaskDependency>()
            .HasOne(td => td.TaskSpecification)
            .WithMany(ts => ts.DependsOn)
            .HasForeignKey(td => new { td.TaskSpecificationId })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClusterProject>()
            .HasIndex(cp => new { cp.ClusterId, cp.ProjectId }).IsUnique();
        modelBuilder.Entity<ClusterProject>()
            .HasOne(cp => cp.Cluster)
            .WithMany(c => c.ClusterProjects)
            .HasForeignKey(cp => new { cp.ClusterId });
        modelBuilder.Entity<ClusterProject>()
            .HasOne(cp => cp.Project)
            .WithMany(p => p.ClusterProjects)
            .HasForeignKey(cp => new { cp.ProjectId });

        modelBuilder.Entity<ClusterProject>()
            .Property(p => p.PreferredAuthType)
            .HasDefaultValue(ClusterAuthenticationCredentialsAuthType.PrivateKey);

        modelBuilder.Entity<ClusterProjectCredential>()
            .HasKey(cpc => new { cpc.ClusterProjectId, cpc.ClusterAuthenticationCredentialsId });
        modelBuilder.Entity<ClusterProjectCredential>()
            .HasOne(cpc => cpc.ClusterProject)
            .WithMany(c => c.ClusterProjectCredentials)
            .HasForeignKey(cpc => new { cpc.ClusterProjectId });
        modelBuilder.Entity<ClusterProjectCredential>()
            .HasOne(cp => cp.ClusterAuthenticationCredentials)
            .WithMany(p => p.ClusterProjectCredentials)
            .HasForeignKey(cp => new { cp.ClusterAuthenticationCredentialsId });

        modelBuilder.Entity<ClusterProjectCredentialCheckLog>()
            .HasIndex(cpccl => new { cpccl.ClusterProjectId, cpccl.CheckTimestamp });

        modelBuilder.Entity<ClusterProjectCredentialCheckLog>()
            .HasOne(cpccl => cpccl.ClusterProjectCredential)
            .WithMany(cpc => cpc.ClusterProjectCredentialsCheckLog)
            .HasForeignKey(cpccl => new { cpccl.ClusterProjectId, cpccl.ClusterAuthenticationCredentialsId });

        modelBuilder.Entity<ClusterAuthenticationCredentials>()
            .Ignore(p => p.Password)
            .Ignore(p => p.PrivateKey)
            .Ignore(p => p.PrivateKeyPassphrase)
            .Ignore(p => p.PrivateKeyCertificate);

        modelBuilder.Entity<ProjectContact>()
            .HasKey(pc => new { pc.ProjectId, pc.ContactId });
        modelBuilder.Entity<ProjectContact>()
            .HasOne(pc => pc.Project)
            .WithMany(p => p.ProjectContacts)
            .HasForeignKey(pc => new { pc.ProjectId });
        modelBuilder.Entity<ProjectContact>()
            .HasOne(pc => pc.Contact)
            .WithMany(p => p.ProjectContacts)
            .HasForeignKey(pc => new { pc.ContactId });

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.AccountingString)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<SubProject>()
            .HasIndex(sp => new { sp.Identifier, sp.ProjectId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<ClusterNodeTypeAggregationAccounting>()
            .HasKey(cna => new { cna.ClusterNodeTypeAggregationId, cna.AccountingId });
        modelBuilder.Entity<ClusterNodeTypeAggregationAccounting>()
            .HasOne(cna => cna.ClusterNodeTypeAggregation)
            .WithMany(cna => cna.ClusterNodeTypeAggregationAccountings)
            .HasForeignKey(cna => cna.ClusterNodeTypeAggregationId);

        modelBuilder.Entity<ProjectClusterNodeTypeAggregation>()
            .HasKey(pcna => new { pcna.ProjectId, pcna.ClusterNodeTypeAggregationId });
        modelBuilder.Entity<ProjectClusterNodeTypeAggregation>()
            .HasOne(pcna => pcna.Project)
            .WithMany(pcna => pcna.ProjectClusterNodeTypeAggregations)
            .HasForeignKey(pcna => pcna.ProjectId);

        modelBuilder.Entity<AdaptorUser>()
            .Property(p => p.UserType).HasDefaultValue(AdaptorUserType.Default);

        modelBuilder.Entity<Cluster>()
            .Property(p => p.CustomConfiguration).HasJsonConversion();

        // Automatic filtering out soft deleted entities (implements ISoftDeletableEntity interface)
        var softDeletableEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(ISoftDeletableEntity).IsAssignableFrom(t.ClrType));

        foreach (var entityType in softDeletableEntityTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(Expression.Equal(
                    Expression.Property(parameter, nameof(ISoftDeletableEntity.IsDeleted)),
                    Expression.Constant(false)),
                parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            
            modelBuilder.Entity(entityType.ClrType)
                .HasIndex([nameof(ISoftDeletableEntity.IsDeleted)])
                .HasFilter("[IsDeleted] = 0");
        }

        modelBuilder.Entity<SubmittedTaskInfo>()
            .HasIndex("SubmittedJobInfoId", nameof(SubmittedTaskInfo.State))
            .IncludeProperties("SpecificationId", "NodeTypeId", "ProjectId");

        modelBuilder.Entity<SubmittedTaskInfo>()
            .HasIndex("SubmittedJobInfoId")
            .IncludeProperties(nameof(SubmittedTaskInfo.State), "NodeTypeId", "SpecificationId");
        
        modelBuilder.Entity<SubmittedTaskInfo>()
            .HasIndex(t => t.State)
            .HasFilter("[State] >= 16")
            .IncludeProperties("ProjectId", "SpecificationId", "SubmittedJobInfoId");
        
        modelBuilder.Entity<SubmittedTaskInfo>()
            .HasIndex(nameof(SubmittedTaskInfo.State), "SubmittedJobInfoId")
            .HasFilter("[State] > 1 AND [State] < 16")
            .IncludeProperties("NodeTypeId", "SpecificationId", "ProjectId");

        modelBuilder.Entity<SubmittedJobInfo>()
            .HasIndex("SpecificationId", "ProjectId")
            .IncludeProperties(nameof(SubmittedJobInfo.State), "SubmitterId");

        modelBuilder.Entity<TaskSpecification>()
            .HasIndex("JobSpecificationId")
            .IncludeProperties("CommandTemplateId", "ClusterNodeTypeId");

        modelBuilder.Entity<SubmittedTaskAllocationNodeInfo>()
            .HasIndex(nameof(SubmittedTaskAllocationNodeInfo.SubmittedTaskInfoId));

        modelBuilder.Entity<SubmittedJobInfo>()
            .HasIndex("ProjectId", nameof(SubmittedJobInfo.StartTime), nameof(SubmittedJobInfo.EndTime));

        modelBuilder.Entity<SubmittedJobInfo>()
            .HasIndex("SubmitterId")
            .IncludeProperties(nameof(SubmittedJobInfo.State));

        modelBuilder.Entity<SubmittedTaskInfo>()
            .HasIndex(t => t.ScheduledJobId)
            .HasFilter("[ScheduledJobId] IS NOT NULL");

        modelBuilder.Entity<ClusterProjectCredentialCheckLog>()
            .HasIndex("ClusterAuthenticationCredentialsId");

        modelBuilder.Entity<ClusterNodeTypeAggregationAccounting>()
            .HasIndex("ClusterNodeTypeAggregationId")
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties("AccountingId");

        modelBuilder.Entity<ClusterProjectCredential>()
            .HasIndex(cpc => cpc.ClusterProjectId)
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties(cpc => cpc.ClusterAuthenticationCredentialsId);

        modelBuilder.Entity<ClusterProject>()
            .HasIndex(cp => cp.ClusterId)
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties(cp => cp.ProjectId);

        modelBuilder.Entity<ClusterProject>()
            .HasIndex(cp => cp.ProjectId)
            .HasFilter("[IsDeleted] = 0")
            .IncludeProperties(cp => cp.ClusterId);
        
        modelBuilder.Entity<SessionCode>()
            .HasIndex(s => s.UniqueCode)
            .IsUnique();
    }
    #endregion

    #region Seeding methods

    private void EnsureDatabaseSeeded()
    {
        EnsureDatabaseSeededAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureDatabaseSeededAsync()
    {
        _logger.LogInformation("Seed data into tha database started.");

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.AdaptorUserRoles);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.AdaptorUsers);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterProxyConnections);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.Clusters?.Select(c => new Cluster
        {
            ConnectionProtocol = c.ConnectionProtocol,
            Description = c.Description,
            Id = c.Id,
            MasterNodeName = c.MasterNodeName,
            DomainName = c.DomainName,
            Port = c.Port,
            Name = c.Name,
            NodeTypes = c.NodeTypes,
            SchedulerType = c.SchedulerType,
            TimeZone = c.TimeZone,
            UpdateJobStateByServiceAccount = c.UpdateJobStateByServiceAccount,
            ProxyConnectionId = c.ProxyConnectionId
        }));

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterAuthenticationCredentials?.Select(cc =>
            new ClusterAuthenticationCredentials
            {
                Id = cc.Id,
                Username = cc.Username,
                Password = cc.Password,
                PrivateKey = cc.PrivateKey,
                PrivateKeyPassphrase = cc.PrivateKeyPassphrase,
                CipherType = cc.CipherType,
                IsDeleted = cc.IsDeleted,
                AuthenticationType = cc.AuthenticationType
            }));

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.FileTransferMethods);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.Accountings);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterNodeTypeAggregations);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterNodeTypeAggregationAccounting, false);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterNodeTypes);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.Projects);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.SubProjects);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.AccountingStates);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ProjectClusterNodeTypeAggregations, false);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.Contacts);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ProjectContacts, false);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterProjects);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.ClusterProjectCredentials, false);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.AdaptorUserGroups);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.AdaptorUserUserGroupRoles, false);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.CommandTemplates);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.CommandTemplateParameters);

        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackInstances);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackDomains);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackProjectDomains);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackProjects);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackAuthenticationCredentials);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackAuthenticationCredentialDomains, false);
        await InsertOrUpdateSeedDataAsync(MiddlewareContextSettings.OpenStackAuthenticationCredentialProjects, false);

        var defaultAccounting = await Set<Accounting>().FirstOrDefaultAsync(a => a.Formula == "1" && !a.IsDeleted);
        if (defaultAccounting == null)
        {
            defaultAccounting = new Accounting
            {
                Formula = "1",
                CreatedAt = DateTime.UtcNow,
                ValidityFrom = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            };
            Set<Accounting>().Add(defaultAccounting);
            SaveChanges();
        }

        var aggregationsWithoutAccounting = await Set<ClusterNodeTypeAggregation>()
            .Where(a => !a.IsDeleted && !Set<ClusterNodeTypeAggregationAccounting>().Any(cna => cna.ClusterNodeTypeAggregationId == a.Id && !cna.IsDeleted))
            .ToListAsync();

        foreach (var agg in aggregationsWithoutAccounting)
        {
            Set<ClusterNodeTypeAggregationAccounting>().Add(new ClusterNodeTypeAggregationAccounting
            {
                ClusterNodeTypeAggregationId = agg.Id,
                AccountingId = defaultAccounting.Id,
                IsDeleted = false
            });
        }
        if (aggregationsWithoutAccounting.Any())
        {
            SaveChanges();
        }

        ValidateSeed();

        SaveChanges();

        var entries = ChangeTracker.Entries();
        entries.ToList().ForEach(e => e.State = EntityState.Detached);

        var clusterAuthCredWithVaultData = await WithVaultDataAsync(await ClusterAuthenticationCredentials
            .Include(x => x.ClusterProjectCredentials)
                .ThenInclude(x => x.ClusterProject)
                    .ThenInclude(x => x.Cluster)
            .ToListAsync());

        foreach (var clusterAuthenticationCredential in clusterAuthCredWithVaultData)
        {
            var clusters = clusterAuthenticationCredential.ClusterProjectCredentials
                .Select(x => x.ClusterProject.Cluster)
                .ToList();
            if (clusters.Count() >= 1)
            {
                var previousAuthType = clusterAuthenticationCredential.AuthenticationType;
                clusterAuthenticationCredential.AuthenticationType =
                    ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(
                        clusterAuthenticationCredential, clusters.First());

                // If auth type changed to SshCertificate but PrivateKey is missing,
                // reset IsInitialized so the system regenerates the key on next use.
                bool isSshCert = clusterAuthenticationCredential.AuthenticationType
                    is ClusterAuthenticationCredentialsAuthType.SshCertificate
                    or ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy;
                bool keyMissing = string.IsNullOrEmpty(clusterAuthenticationCredential.PrivateKey);
                if (isSshCert && keyMissing)
                {
                    foreach (var cpc in clusterAuthenticationCredential.ClusterProjectCredentials)
                    {
                        cpc.IsInitialized = false;
                    }
                    _logger.LogWarning(
                        "Credential ID {Id} (Username: {Username}) has AuthenticationType {AuthType} but missing PrivateKey. " +
                        "Resetting IsInitialized to trigger re-provisioning.",
                        clusterAuthenticationCredential.Id,
                        clusterAuthenticationCredential.Username,
                        clusterAuthenticationCredential.AuthenticationType);
                }
            }
        }
        SaveChanges();
        _logger.LogInformation("Seed data into the database completed.");
    }

    private void ValidateSeed()
    {
        _logger.LogInformation("Seed validation has started.");
        ValidateCommandTemplateToProjectReference(MiddlewareContextSettings.CommandTemplates,
            MiddlewareContextSettings.ClusterProjects);
        ValidateClusterAuthenticationCredentialsClusterReference(MiddlewareContextSettings
            .ClusterAuthenticationCredentials);
        ValidateProjectContactReferences(MiddlewareContextSettings.ProjectContacts);
        _logger.LogInformation("Seed validation completed.");
    }

    private async Task<List<ClusterAuthenticationCredentials>> WithVaultDataAsync(
        IEnumerable<ClusterAuthenticationCredentials> credentials)
    {
        if (credentials == null) return new List<ClusterAuthenticationCredentials>();
        var materialized = credentials.ToList();
        var _vaultConnector = new VaultConnector(_logger);
        foreach (var item in materialized)
        {
            var vaultData = await _vaultConnector.GetClusterAuthenticationCredentials(item.Id);
            item.ImportVaultData(vaultData);
        }

        return materialized;
    }

    private void ValidateProjectContactReferences(List<ProjectContact> projectContacts)
    {
        foreach (var projectContact in projectContacts.GroupBy(x => x.ProjectId))
            if (projectContact.Count(x => x.IsPI) > 1)
                throw new DbContextException("MaxPICount", projectContact.Key);
    }

    private void ValidateClusterAuthenticationCredentialsClusterReference(
        List<ClusterAuthenticationCredentials> clusterAuthenticationCredentials)
    {
        var clusterProjectById = MiddlewareContextSettings.ClusterProjects
            .ToDictionary(cp => cp.Id);
        var clusterById = MiddlewareContextSettings.Clusters
            .ToDictionary(c => c.Id);

        foreach (var cred in clusterAuthenticationCredentials)
        {
            var credClusterIds = MiddlewareContextSettings.ClusterProjectCredentials
                .Where(cpc => cpc.ClusterAuthenticationCredentialsId == cred.Id)
                .Select(cpc => cpc.ClusterProjectId)
                .Distinct()
                .Select(cpId => clusterProjectById.TryGetValue(cpId, out var cp) ? (long?)cp.ClusterId : null)
                .Where(cId => cId.HasValue)
                .Select(cId => cId!.Value)
                .ToList();

            var proxyIds = credClusterIds
                .Select(cId => clusterById.TryGetValue(cId, out var cl) ? cl.ProxyConnectionId : null)
                .Distinct()
                .ToList();

            if (proxyIds.Count > 1)
                throw new DbContextException("CredentialsProxyMismatch", cred.Id);
        }
    }

    private void ValidateCommandTemplateToProjectReference(List<CommandTemplate> commandTemplates,
        List<ClusterProject> clusterProjects)
    {
        var commandTemplatesWithProjectReference = commandTemplates.Where(x => x.ProjectId.HasValue);
        foreach (var commandTemplate in commandTemplatesWithProjectReference)
            if (!clusterProjects.Any(x =>
                    x.ClusterId == commandTemplate.ClusterNodeType.ClusterId &&
                    x.ProjectId == commandTemplate.ProjectId))
                throw new DbContextException("NotExistingClusterProjectReference", commandTemplate.Id,
                    commandTemplate.ProjectId);
    }

    private async Task InsertOrUpdateSeedDataAsync<T>(IEnumerable<T> items, bool useSetIdentity = true) where T : class
    {
        if (items == null || !items.Any()) return;

        var tableName = Model.FindEntityType(typeof(T)).GetTableName();
        _logger.LogInformation($"Inserting or updating seed data into {tableName} is initiated.");

        await Database.OpenConnectionAsync();
        try
        {
            foreach (var item in items)
                await AddOrUpdateItem(item);

            if (useSetIdentity)
            {
                var executionStrategy = Database.CreateExecutionStrategy();
                executionStrategy.Execute(() =>
                {
                    using var transaction = Database.BeginTransaction();
                    try
                    {
                        using var command = Database.GetDbConnection().CreateCommand();
                        command.Transaction = transaction.GetDbTransaction();
                        command.CommandText = $"SET IDENTITY_INSERT [{tableName}] ON;";
                        if (command.Connection.State != ConnectionState.Open) command.Connection.Open();
                        command.ExecuteNonQuery();

                        SaveChanges();

                        command.CommandText = $"SET IDENTITY_INSERT [{tableName}] OFF;";
                        command.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error inserting seed data with IDENTITY_INSERT for {tableName}");
                        transaction.Rollback();
                        throw;
                    }
                });
            }
            else
            {
                SaveChanges();
            }
        }
        catch (Exception e)
        {
            await Database.CloseConnectionAsync();
            _logger.LogError($"Inserting or updating seed into {tableName} is not completed. Error message: \"{e.Message}\"");
        }
        finally
        {
            await Database.CloseConnectionAsync();
            _logger.LogInformation($"Inserting or updating seed into {tableName} is completed.");
        }
    }

    public void UpdateEntityOrAddItem<T>(T entity, T item) where T : class
    {
        if (entity != null)
        {
            Entry(entity).State = EntityState.Detached;
            Entry(item).State = EntityState.Modified;
        }
        else
        {
            Set<T>().Add(item);
        }
    }

    private async Task AddOrUpdateItem<T>(T item) where T : class
    {
        switch (item)
        {
            case IdentifiableDbEntity identifiableItem:
            {
                var entity = Set<T>().OfType<IdentifiableDbEntity>().IgnoreQueryFilters().FirstOrDefault(c => c.Id == identifiableItem.Id);
                UpdateEntityOrAddItem((T)(object)entity, item);
                var entity_after_update = Set<T>().Find(identifiableItem.Id);

                if (entity_after_update is ClusterAuthenticationCredentials clusterProjectCredentialEntity)
                {
                    var vaultConnector = new VaultConnector(_logger);
                    var vaultData = await vaultConnector
                        .GetClusterAuthenticationCredentials(clusterProjectCredentialEntity.Id);

                    _logger.LogInformation(vaultData.Id > 0
                        ? $"Vault data for ClusterAuthenticationCredentials with id {clusterProjectCredentialEntity.Id} found. Setting credentials."
                        : $"Vault data for ClusterAuthenticationCredentials with id {(item as ClusterAuthenticationCredentials)!.Id} not found. Creating new credentials.");
                    var newVaultData = (item as ClusterAuthenticationCredentials)!.ExportVaultData();
                    await vaultConnector.SetClusterAuthenticationCredentialsAsync(newVaultData);
                }

                break;
            }

            case AdaptorUserUserGroupRole userGroupItem:
            {
                var entity = Set<T>().OfType<AdaptorUserUserGroupRole>().IgnoreQueryFilters().FirstOrDefault(c =>
                    c.AdaptorUserId == userGroupItem.AdaptorUserId &&
                    c.AdaptorUserGroupId == userGroupItem.AdaptorUserGroupId &&
                    c.AdaptorUserRoleId == userGroupItem.AdaptorUserRoleId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case OpenStackAuthenticationCredentialProject openstackCredProject:
            {
                var entity = Set<T>().OfType<OpenStackAuthenticationCredentialProject>().IgnoreQueryFilters().FirstOrDefault(c =>
                    c.OpenStackAuthenticationCredentialId == openstackCredProject.OpenStackAuthenticationCredentialId &&
                    c.OpenStackProjectId == openstackCredProject.OpenStackProjectId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case OpenStackAuthenticationCredentialDomain openstackCredDomain:
            {
                var entity = Set<T>().OfType<OpenStackAuthenticationCredentialDomain>().IgnoreQueryFilters().FirstOrDefault(c =>
                    c.OpenStackAuthenticationCredentialId == openstackCredDomain.OpenStackAuthenticationCredentialId &&
                    c.OpenStackDomainId == openstackCredDomain.OpenStackDomainId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case ClusterProjectCredential clusterProjectCredentials:
            {
                var entity = Set<T>().OfType<ClusterProjectCredential>().IgnoreQueryFilters().FirstOrDefault(c =>
                    c.ClusterProjectId == clusterProjectCredentials.ClusterProjectId &&
                    c.ClusterAuthenticationCredentialsId == clusterProjectCredentials.ClusterAuthenticationCredentialsId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case ProjectContact projectContact:
            {
                var entity = Set<T>().OfType<ProjectContact>().IgnoreQueryFilters().FirstOrDefault(c =>
                    c.ProjectId == projectContact.ProjectId && c.ContactId == projectContact.ContactId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case ClusterNodeTypeAggregationAccounting clusterNodeTypeAggregationAccounting:
            {
                var entity = Set<T>().OfType<ClusterNodeTypeAggregationAccounting>()
                    .IgnoreQueryFilters()
                    .FirstOrDefault(c =>
                        c.ClusterNodeTypeAggregationId ==
                        clusterNodeTypeAggregationAccounting.ClusterNodeTypeAggregationId &&
                        c.AccountingId == clusterNodeTypeAggregationAccounting.AccountingId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            case ProjectClusterNodeTypeAggregation projectClusterNodeTypeAggregation:
            {
                var entity = Set<T>().OfType<ProjectClusterNodeTypeAggregation>()
                    .IgnoreQueryFilters()
                    .FirstOrDefault(c =>
                        c.ClusterNodeTypeAggregationId ==
                        projectClusterNodeTypeAggregation.ClusterNodeTypeAggregationId &&
                        c.ProjectId == projectClusterNodeTypeAggregation.ProjectId);
                UpdateEntityOrAddItem((T)(object)entity, item);
                break;
            }
            default:
                throw new DbContextException("NotSupportedSeedEntity", typeof(T).Name);
        }
    }

    #endregion

    #region Entities

    #region ClusterInformation Entities

    public virtual DbSet<ClusterProxyConnection> ClusterProxyConnections { get; set; }
    public virtual DbSet<Cluster> Clusters { get; set; }
    public virtual DbSet<ClusterAuthenticationCredentials> ClusterAuthenticationCredentials { get; set; }
    public virtual DbSet<ClusterNodeType> ClusterNodeTypes { get; set; }

    #endregion

    #region OpenStack Entities

    public virtual DbSet<OpenStackAuthenticationCredential> OpenStackAuthenticationCredentials { get; set; }
    public virtual DbSet<OpenStackInstance> OpenStackInstances { get; set; }
    public virtual DbSet<OpenStackDomain> OpenStackDomains { get; set; }
    public virtual DbSet<OpenStackProject> OpenStackProjects { get; set; }

    #endregion

    #region FileTransfer Entities

    public virtual DbSet<FileSpecification> FileSpecifications { get; set; }
    public virtual DbSet<FileTransferMethod> FileTransferMethods { get; set; }

    #endregion

    #region JobManagement.JobInformation Entities

    public virtual DbSet<SubmittedJobInfo> SubmittedJobInfos { get; set; }
    public virtual DbSet<SubmittedTaskInfo> SubmittedTaskInfos { get; set; }
    public virtual DbSet<TaskDependency> TaskDependencyInfos { get; set; }

    #endregion

    #region JobManagement Entities

    public virtual DbSet<CommandTemplate> CommandTemplates { get; set; }
    public virtual DbSet<CommandTemplateParameter> CommandTemplateParameters { get; set; }
    public virtual DbSet<CommandTemplateParameterValue> CommandTemplateParameterValues { get; set; }
    public virtual DbSet<EnvironmentVariable> EnvironmentVariables { get; set; }
    public virtual DbSet<JobSpecification> JobSpecifications { get; set; }
    public virtual DbSet<TaskSpecification> TaskSpecifications { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<SubProject> SubProjects { get; set; }
    public virtual DbSet<Contact> Contacts { get; set; }
    public virtual DbSet<ClusterProject> ClusterProjects { get; set; }
    public virtual DbSet<ClusterProjectCredential> ClusterProjectCredentials { get; set; }
    public virtual DbSet<ClusterProjectCredentialCheckLog> ClusterProjectCredentialsCheckLog { get; set; }

    #endregion

    #region UserAndLimitationManagement Entities

    public virtual DbSet<AdaptorUser> AdaptorUsers { get; set; }
    public virtual DbSet<AdaptorUserGroup> AdaptorUserGroups { get; set; }
    public virtual DbSet<AdaptorUserUserGroupRole> AdaptorUserUserGroups { get; set; }
    public virtual DbSet<AdaptorUserRole> AdaptorUserRoles { get; set; }
    public virtual DbSet<SessionCode> SessionCodes { get; set; }
    public virtual DbSet<OpenStackSession> OpenStackSessions { get; set; }

    #endregion

    #endregion
}
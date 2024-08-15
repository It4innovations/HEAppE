using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HEAppE.DataAccessTier.Vault;
using HEAppE.DataAccessTier.Configuration;
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

using log4net;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier
{
    internal class MiddlewareContext : DbContext
    {
        #region Instances
        private static readonly object _lockObject = new object();
        private static volatile bool _isMigrated = false;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        #region Constructors
        public MiddlewareContext() : base()
        {
            if (!_isMigrated)
            {
                lock (_lockObject)
                {
                    if (!_isMigrated)
                    {
                        try
                        {
                            string localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
                            if (localRunEnv != "LocalWindows")
                            {
                                // Connection to Database works and Database not exist
                                if (!Database.CanConnect())
                                {
                                    _log.Info("Starting migration and seeding into the new database.");
                                    Database.Migrate();
                                    EnsureDatabaseSeeded();
                                    _isMigrated = true;
                                }
                                else
                                {
                                    var lastAppliedMigration = Database.GetAppliedMigrations().LastOrDefault();
                                    var lastDefinedMigration = Database.GetMigrations().LastOrDefault();

                                    if (lastAppliedMigration is null)
                                    {
                                        _log.Info("Starting migration into the new database.");
                                        Database.Migrate();
                                        lastAppliedMigration = Database.GetAppliedMigrations().LastOrDefault();
                                    }
                                    else if (DatabaseMigrationSettings.AutoMigrateDatabase && lastAppliedMigration != lastDefinedMigration)
                                    {
                                        _log.Info("Applying newer migrations to the database.");
                                        Database.Migrate();
                                        _isMigrated = true;
                                    }
                                    else if (lastAppliedMigration != lastDefinedMigration)
                                    {
                                        throw new DbContextException("MigrationMismatch");
                                    }

                                    if (Database.GetAppliedMigrations().Count() != Database.GetMigrations().Count())
                                    {
                                        throw new DbContextException("MigrationCountMismatch");
                                    }

                                    _log.Info("Application and database migrations are the same. Starting seeding data into the database.");
                                    EnsureDatabaseSeeded();
                                    _isMigrated = true;
                                }
                            }
                        }
                        catch (SqlException ex)
                        {
                            throw new DbContextException("MigrationError", ex);
                        }
                    }
                }
            }
        }
        #endregion
        #region Override Methods
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
            optionsBuilder.UseSqlServer(MiddlewareContextSettings.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //M:N relations for AdaptorUserUserGroupRole
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

            //M:N relations for OpenStackAuthenticationCredentialDomain
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

            //M:N relations for OpenStackAuthenticationCredentialProject
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

            //M:N relations for TaskDependency for same table TaskSpecification
            //Cascade delete or update are not allowed
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

            //M:N relations for ClusterProject and unique constraint
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

            //M:N relations for ClusterProjectCredentials
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

            //M:N ClusterProjectCredentials ignore Vault properties
            modelBuilder.Entity<ClusterAuthenticationCredentials>()
                //.Ignore(c => c.AuthenticationType)
                //.Ignore(p => p.CipherType)
                .Ignore(p => p.Password)
                .Ignore(p => p.PrivateKey)
                .Ignore(p => p.PrivateKeyPassphrase)
                .Ignore(p => p.PrivateKeyCertificate);

            //M:N relations for ProjectContact
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
                .IsUnique();
            
            //Subproject Identifier and ProjectId unique constraint
            modelBuilder.Entity<SubProject>()
                .HasIndex(sp => new { sp.Identifier, sp.ProjectId })
                .IsUnique();
            
            //M:N relations for ClusterNodeTypeAggregationAccounting
            modelBuilder.Entity<ClusterNodeTypeAggregationAccounting>()
                .HasKey(cna => new { cna.ClusterNodeTypeAggregationId, cna.AccountingId });
            modelBuilder.Entity<ClusterNodeTypeAggregationAccounting>()
                .HasOne(cna => cna.ClusterNodeTypeAggregation)
                .WithMany(cna => cna.ClusterNodeTypeAggregationAccountings)
                .HasForeignKey(cna => cna.ClusterNodeTypeAggregationId);
            
            //M:N relations for ProjectClusterNodeTypeAggregation
            modelBuilder.Entity<ProjectClusterNodeTypeAggregation>()
                .HasKey(pcna => new { pcna.ProjectId, pcna.ClusterNodeTypeAggregationId });
            modelBuilder.Entity<ProjectClusterNodeTypeAggregation>()
                .HasOne(pcna => pcna.Project)
                .WithMany(pcna => pcna.ProjectClusterNodeTypeAggregations)
                .HasForeignKey(pcna => pcna.ProjectId);

            modelBuilder.Entity<AdaptorUser>()
                .Property(p => p.UserType).HasDefaultValue(AdaptorUserType.Default);
        }
        #endregion
        #region Seeding methods
        //Should not be called from more instances on one database -> concurrency issues
        //Does not contain modification of existing data or adding new records
        private void EnsureDatabaseSeeded()
        {
            _log.Info("Seed data into tha database started.");

            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserRoles);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUsers);

            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterProxyConnections);
            InsertOrUpdateSeedData(MiddlewareContextSettings.Clusters?.Select(c => new Cluster
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

            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterAuthenticationCredentials?.Select(cc => new ClusterAuthenticationCredentials
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

            InsertOrUpdateSeedData(MiddlewareContextSettings.FileTransferMethods);
            
            InsertOrUpdateSeedData(MiddlewareContextSettings.Accountings);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterNodeTypeAggregations);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterNodeTypeAggregationAccounting, false);
            
            
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterNodeTypes);

            InsertOrUpdateSeedData(MiddlewareContextSettings.Projects);
            InsertOrUpdateSeedData(MiddlewareContextSettings.SubProjects);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AccountingStates);
            
            InsertOrUpdateSeedData(MiddlewareContextSettings.ProjectClusterNodeTypeAggregations, false);
            InsertOrUpdateSeedData(MiddlewareContextSettings.Contacts);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ProjectContacts, false);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterProjects);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterProjectCredentials, false);

            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserGroups);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserUserGroupRoles, false);

            InsertOrUpdateSeedData(MiddlewareContextSettings.CommandTemplates);
            InsertOrUpdateSeedData(MiddlewareContextSettings.CommandTemplateParameters);

            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackInstances);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackDomains);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackProjectDomains);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackProjects);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackAuthenticationCredentials);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackAuthenticationCredentialDomains, false);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackAuthenticationCredentialProjects, false);

            ValidateSeed();

            SaveChanges();

            var entries = ChangeTracker.Entries();
            //Prevents duplicit entries in memory when items updated
            entries.ToList().ForEach(e => e.State = EntityState.Detached);

            //Update Authentication type

            var clusterAuthCredWithVaultData = WithVaultData(ClusterAuthenticationCredentials);

            clusterAuthCredWithVaultData.ToList().ForEach(clusterAuthenticationCredential =>
            {
                var clusters = clusterAuthenticationCredential.ClusterProjectCredentials
                                                                .Select(x => x.ClusterProject.Cluster)
                                                                .ToList();
                if (clusters.Count() >= 1)
                {
                    clusterAuthenticationCredential.AuthenticationType = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(clusterAuthenticationCredential, clusters.First());
                }
            });
            SaveChanges();
            _log.Info("Seed data into the database completed.");
        }

        private void ValidateSeed()
        {
            _log.Info("Seed validation has started.");
            ValidateCommandTemplateToProjectReference(MiddlewareContextSettings.CommandTemplates, MiddlewareContextSettings.ClusterProjects);
            ValidateClusterAuthenticationCredentialsClusterReference(MiddlewareContextSettings.ClusterAuthenticationCredentials);
            ValidateProjectContactReferences(MiddlewareContextSettings.ProjectContacts);
            _log.Info("Seed validation completed.");
        }

        private IEnumerable<ClusterAuthenticationCredentials> WithVaultData(IEnumerable<ClusterAuthenticationCredentials> credentials)
        {
            if (credentials == null)
            {
                return Enumerable.Empty<ClusterAuthenticationCredentials>();
            }
            var _vaultConnector = new VaultConnector();
            foreach (var item in credentials)
            {
                var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(item.Id).GetAwaiter().GetResult();
                item.ImportVaultData(vaultData);
            }
            return credentials;
        }

        /// <summary>
        /// Validate CommandTemplate to Project reference
        /// </summary>
        /// <param name="projectContacts"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ValidateProjectContactReferences(List<ProjectContact> projectContacts)
        {
            foreach (var projectContact in projectContacts.GroupBy(x => x.ProjectId))
            {
                //if project contact has more than one PI throw exception
                if (projectContact.Count(x => x.IsPI) > 1)
                {
                    throw new DbContextException("MaxPICount", projectContact.Key);
                }
            }
        }

        /// <summary>
        /// Validate ClusterAuthenticationCredentials to used clusters same proxy connection
        /// </summary>
        /// <param name="clusterAuthenticationCredentials"></param>
        /// <exception cref="ApplicationException"></exception>
        private void ValidateClusterAuthenticationCredentialsClusterReference(List<ClusterAuthenticationCredentials> clusterAuthenticationCredentials)
        {
            foreach (var clusterAuthenticationCredential in clusterAuthenticationCredentials)
            {
                var clusters = clusterAuthenticationCredential.ClusterProjectCredentials.Select(x => x.ClusterProject.Cluster).ToList();
                if (clusters.Count() >= 1 && clusters.Any(c => c.ProxyConnection != clusters.First().ProxyConnection))
                {
                    throw new DbContextException("CredentialsProxyMismatch", clusterAuthenticationCredential.Id);
                }
            }
        }

        /// <summary>
        /// Validate CommandTemplate to Projectcross reference to ClusterProject mapping
        /// </summary>
        /// <param name="commandTemplates">All Command Templates</param>
        /// <param name="clusterProjects">All ClusterProject</param>
        /// <exception cref="ApplicationException"></exception>
        private void ValidateCommandTemplateToProjectReference(List<CommandTemplate> commandTemplates, List<ClusterProject> clusterProjects)
        {
            //check if exists ClusterProject reference if CommandTemplate is referenced to some project
            var commandTemplatesWithProjectReference = commandTemplates.Where(x => x.ProjectId.HasValue);
            foreach (var commandTemplate in commandTemplatesWithProjectReference)
            {
                //if does not exist Cluster to Project reference, throw exception
                if (!clusterProjects.Any(x => x.ClusterId == commandTemplate.ClusterNodeType.ClusterId && x.ProjectId == commandTemplate.ProjectId))
                {
                    throw new DbContextException("NotExistingClusterProjectReference", commandTemplate.Id, commandTemplate.ProjectId);
                }
            }
        }

        //sqlserver specific because of identity
        private void InsertOrUpdateSeedData<T>(IEnumerable<T> items, bool useSetIdentity = true) where T : class
        {
            if (items == null || items.Count() == 0)
            {
                return;
            }

            var tableName = Model.FindEntityType(typeof(T)).GetTableName();
            _log.Info($"Inserting or updating seed data into {tableName} is initiated.");

            Database.OpenConnection();
            try
            {
                foreach (var item in items)
                {
                    AddOrUpdateItem(item);
                }

                if (useSetIdentity)
                {
                    Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} ON;");
                    SaveChanges();
                    Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} OFF;");
                }
                else
                {
                    SaveChanges();
                }
            }
            catch (Exception e)
            {
                Database.CloseConnection();
                _log.Error($"Inserting or updating seed into {tableName} is not completed. Error message: \"{e.Message}\"");
            }
            finally
            {
                Database.CloseConnection();
                _log.Info($"Inserting or updating seed into {tableName} is completed.");
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

        private void AddOrUpdateItem<T>(T item) where T : class
        {
            switch (item)
            {
                case IdentifiableDbEntity identifiableItem:
                {
                    var entity = Set<T>().Find(identifiableItem.Id);

                    if (entity is ClusterAuthenticationCredentials clusterProjectCredentialEntity)
                    {
                        var vaultConnector = new VaultConnector();
                        var vaultData = vaultConnector.GetClusterAuthenticationCredentials(clusterProjectCredentialEntity.Id).GetAwaiter().GetResult();

                        if (vaultData.Id > 0)
                        {
                            _log.Info($"Vault data for ClusterAuthenticationCredentials with id {clusterProjectCredentialEntity.Id} found. Setting credentials.");
                            vaultConnector.SetClusterAuthenticationCredentials(vaultData);
                        }
                        else
                        {
                            _log.Info($"Vault data for ClusterAuthenticationCredentials with id {clusterProjectCredentialEntity.Id} not found. Creating new credentials.");
                            var newVaultData = clusterProjectCredentialEntity.ExportVaultData();
                            vaultConnector.SetClusterAuthenticationCredentials(newVaultData);
                        }
                    }

                    UpdateEntityOrAddItem(entity, item);
                    break;
                }

                case AdaptorUserUserGroupRole userGroupItem:
                {
                    var entity = Set<T>().Find(userGroupItem.AdaptorUserId, userGroupItem.AdaptorUserGroupId, userGroupItem.AdaptorUserRoleId);
                    UpdateEntityOrAddItem(entity, item);
                    break;
                }
                case OpenStackAuthenticationCredentialProject openstackCredProject:
                {
                    var entity = Set<T>().Find(openstackCredProject.OpenStackAuthenticationCredentialId, openstackCredProject.OpenStackProjectId);
                    UpdateEntityOrAddItem(entity, item);
                    break;
                }
                case OpenStackAuthenticationCredentialDomain openstackCredDomain:
                {
                    var entity = Set<T>().Find(openstackCredDomain.OpenStackAuthenticationCredentialId, openstackCredDomain.OpenStackDomainId);
                    UpdateEntityOrAddItem(entity, item);
                    break;
                }
                case ClusterProjectCredential clusterProjectCredentials:
                {
                    var entity = Set<T>().Find(clusterProjectCredentials.ClusterProjectId, clusterProjectCredentials.ClusterAuthenticationCredentialsId);
                    UpdateEntityOrAddItem(entity, item);
                    break;
                }
                case ProjectContact projectContact:
                    {
                        var entity = Set<T>().Find(projectContact.ProjectId, projectContact.ContactId);
                        UpdateEntityOrAddItem(entity, item);
                        break;
                    }
                case ClusterNodeTypeAggregationAccounting clusterNodeTypeAggregationAccounting:
                    {
                        var entity = Set<T>().Find(clusterNodeTypeAggregationAccounting.ClusterNodeTypeAggregationId, clusterNodeTypeAggregationAccounting.AccountingId);
                        UpdateEntityOrAddItem(entity, item);
                        break;
                    }
                case ProjectClusterNodeTypeAggregation projectClusterNodeTypeAggregation:
                    {
                        var entity = Set<T>().Find(projectClusterNodeTypeAggregation.ProjectId, projectClusterNodeTypeAggregation.ClusterNodeTypeAggregationId);
                        UpdateEntityOrAddItem(entity, item);
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
}
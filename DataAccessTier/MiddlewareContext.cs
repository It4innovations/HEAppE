using HEAppE.DomainObjects;
using HEAppE.DomainObjects.AdminUserManagement;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.Notifications;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Reflection;
using System;
using HEAppE.DomainObjects.OpenStack;


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
                                if (lastAppliedMigration == lastDefinedMigration)
                                {
                                    _log.Info("Application and database migrations are same. Starting seeding data into database.");
                                    EnsureDatabaseSeeded();
                                    _isMigrated = true;
                                }
                                else
                                {
                                    _log.Error("Application and database migrations are not the same. Please update the database to the new version.");
                                }
                            }
                        }
                        catch (SqlException ex)
                        {
                            _log.Error("There is not connection to database server.");
                            throw new ApplicationException("Error occuers in database migrations.", ex);
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

            //M:N relations for AdaptorUserUserGroup
            modelBuilder.Entity<AdaptorUserUserGroup>()
                .HasKey(ug => new { ug.AdaptorUserId, ug.AdaptorUserGroupId });
            modelBuilder.Entity<AdaptorUserUserGroup>()
                .HasOne(ug => ug.AdaptorUser)
                .WithMany(u => u.AdaptorUserUserGroups)
                .HasForeignKey(ug => new { ug.AdaptorUserId });
            modelBuilder.Entity<AdaptorUserUserGroup>()
                .HasOne(ug => ug.AdaptorUserGroup)
                .WithMany(g => g.AdaptorUserUserGroups)
                .HasForeignKey(ug => new { ug.AdaptorUserGroupId });

            // TODO(Moravec): This should make role name unique, but it doesn't work for me?
            modelBuilder.Entity<AdaptorUserRole>().HasAlternateKey(x => x.Name);

            // M:N relations for AdaptorUserUserRole
            modelBuilder.Entity<AdaptorUserUserRole>()
                .HasKey(userRole => new { userRole.AdaptorUserId, userRole.AdaptorUserRoleId });
            modelBuilder.Entity<AdaptorUserUserRole>()
                .HasOne(userRole => userRole.AdaptorUser)
                .WithMany(userRole => userRole.AdaptorUserUserRoles)
                .HasForeignKey(userRoles => new { userRoles.AdaptorUserId });
            modelBuilder.Entity<AdaptorUserUserRole>()
                .HasOne(userRole => userRole.AdaptorUserRole)
                .WithMany(userRole => userRole.AdaptorUserUserRoles)
                .HasForeignKey(userRoles => new { userRoles.AdaptorUserRoleId });

            //M:N relations for AdministrationUserRole
            modelBuilder.Entity<AdministrationUserRole>()
                .HasKey(ur => new { ur.AdministrationRoleId, ur.AdministrationUserId });
            modelBuilder.Entity<AdministrationUserRole>()
                .HasOne(ur => ur.AdministrationRole)
                .WithMany(u => u.AdministrationUserRoles)
                .HasForeignKey(ur => new { ur.AdministrationRoleId });
            modelBuilder.Entity<AdministrationUserRole>()
                .HasOne(ur => ur.AdministrationUser)
                .WithMany(g => g.AdministrationUserRoles)
                .HasForeignKey(ur => new { ur.AdministrationUserId });

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
        }
        #endregion
        #region Seeding methods
        //Should not be called from more instances on one database -> concurrency issues
        //Does not contain modification of existing data or adding new records
        private void EnsureDatabaseSeeded()
        {
            _log.Info("Seed data into tha database started.");

            InsertOrUpdateSeedData(MiddlewareContextSettings.Languages);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUsers);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserRoles);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserGroups);
            InsertOrUpdateSeedData(MiddlewareContextSettings.AdaptorUserUserGroups, false);
            InsertOrUpdateSeedData(MiddlewareContextSettings.Clusters?.Select(c => new Cluster
            {
                AuthenticationCredentials = c.AuthenticationCredentials,
                ConnectionProtocol = c.ConnectionProtocol,
                Description = c.Description,
                Id = c.Id,
                MasterNodeName = c.MasterNodeName,
                Name = c.Name,
                NodeTypes = c.NodeTypes,
                SchedulerType = c.SchedulerType,
                LocalBasepath = c.LocalBasepath,
                TimeZone = c.TimeZone
            }));
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterAuthenticationCredentials);
            InsertOrUpdateSeedData(MiddlewareContextSettings.FileTransferMethods);
            InsertOrUpdateSeedData(MiddlewareContextSettings.JobTemplates);
            InsertOrUpdateSeedData(MiddlewareContextSettings.TaskTemplates);
            InsertOrUpdateSeedData(MiddlewareContextSettings.ClusterNodeTypes);
            InsertOrUpdateSeedData(MiddlewareContextSettings.CommandTemplates);
            InsertOrUpdateSeedData(MiddlewareContextSettings.CommandTemplateParameters);
            InsertOrUpdateSeedData(MiddlewareContextSettings.PropertyChangeSpecifications);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackInstances);
            InsertOrUpdateSeedData(MiddlewareContextSettings.OpenStackAuthenticationCredentials);

            //Update Cluster foreign keys which could not be added before
            MiddlewareContextSettings.Clusters?.ForEach(c => Clusters.Find(c.Id).ServiceAccountCredentialsId = c.ServiceAccountCredentialsId);
            SaveChanges();

            var entries = ChangeTracker.Entries();
            //Prevents duplicit entries in memory when items updated
            entries.ToList().ForEach(e => e.State = EntityState.Detached);

            SaveChanges();
            _log.Info("Seed data into the database completed.");
        }

        //sqlserver specific because of identity
        private void InsertOrUpdateSeedData<T>(IEnumerable<T> items, bool useSetIdentity = true) where T : class
        {
            if (items == null)
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
            finally
            {
                Database.CloseConnection();
                _log.Info($"Inserting or updating seed into {tableName} is completed.");
            }
        }

        private void AddOrUpdateItem<T>(T item) where T : class
        {
            if (item is IdentifiableDbEntity identifiableItem)
            {
                var entity = Set<T>().Find(identifiableItem.Id);
                UpdateEntityOrAddItem(entity, item);
            }
            else if (item is AdaptorUserUserGroup userGroupItem)
            {
                var entity = Set<T>().Find(userGroupItem.AdaptorUserId, userGroupItem.AdaptorUserGroupId);
                UpdateEntityOrAddItem(entity, item);
            }
            else
            {
                throw new ApplicationException("Seed entity is not supported.");
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
        #endregion
        #region Entities
        #region AdminUserManagement Entities
        public virtual DbSet<AdministrationRole> AdministrationRoles { get; set; }
        public virtual DbSet<AdministrationUser> AdministrationUsers { get; set; }
        #endregion

        #region ClusterInformation Entities
        public virtual DbSet<Cluster> Clusters { get; set; }
        public virtual DbSet<ClusterAuthenticationCredentials> ClusterAuthenticationCredentials { get; set; }
        public virtual DbSet<ClusterNodeType> ClusterNodeTypes { get; set; }
        #endregion

        #region OpenStack Entities
        public virtual DbSet<OpenStackInstance> OpenStackInstances { get; set; }
        public virtual DbSet<OpenStackAuthenticationCredentials> OpenStackAuthenticationCredentials { get; set; }
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
        public virtual DbSet<JobTemplate> JobTemplates { get; set; }
        public virtual DbSet<TaskTemplate> TaskTemplates { get; set; }
        public virtual DbSet<PropertyChangeSpecification> PropertyChangeSpecifications { get; set; }
        public virtual DbSet<TaskSpecification> TaskSpecifications { get; set; }
        #endregion

        #region Notifications Entities
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<MessageLocalization> MessageLocalizations { get; set; }
        public virtual DbSet<MessageTemplate> MessageTemplates { get; set; }
        public virtual DbSet<MessageTemplateParameter> MessageTemplateParameters { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        #endregion

        #region UserAndLimitationManagement Entities
        public virtual DbSet<AdaptorUser> AdaptorUsers { get; set; }
        public virtual DbSet<AdaptorUserGroup> AdaptorUserGroups { get; set; }
        public virtual DbSet<AdaptorUserUserGroup> AdaptorUserUserGroups { get; set; }
        public virtual DbSet<AdaptorUserRole> AdaptorUserRoles { get; set; }
        public virtual DbSet<AdaptorUserUserRole> AdaptorUserUserRoles { get; set; }
        public virtual DbSet<ResourceLimitation> ResourceLimitations { get; set; }
        public virtual DbSet<SessionCode> SessionCodes { get; set; }
        public virtual DbSet<OpenStackSession> OpenStackSessions { get; set; }
        #endregion
        #endregion
    }
}
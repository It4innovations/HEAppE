using HEAppE.CertificateGenerator;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using log4net;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Transactions;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public class ManagementLogic : IManagementLogic
    {
        protected IUnitOfWork _unitOfWork;
        protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Script Configuration
        /// </summary>
        protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;
        public ManagementLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }



        /// <summary>
        /// Create a command template based on a generic command template
        /// </summary>
        /// <param name="genericCommandTemplateId"></param>
        /// <param name="name"></param>
        /// <param name="projectId"></param>
        /// <param name="description"></param>
        /// <param name="extendedAllocationCommand"></param>
        /// <param name="executableFile"></param>
        /// <param name="preparationScript"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="InputValidationException"></exception>
        public CommandTemplate CreateCommandTemplateFromGeneric(long genericCommandTemplateId, string name,
            long projectId, string description, string extendedAllocationCommand, string executableFile,
            string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(genericCommandTemplateId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException($"ProjectNotFound");
            }

            if (commandTemplate == null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            if (!commandTemplate.IsGeneric)
            {
                throw new InputValidationException("CommandTemplateNotGeneric");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }

            CommandTemplateParameter commandTemplateParameter =
                commandTemplate.TemplateParameters.FirstOrDefault(f => f.IsVisible);
            if (string.IsNullOrEmpty(commandTemplateParameter?.Identifier))
            {
                throw new RequestedObjectDoesNotExistException("UserScriptNotDefined");
            }

            if (string.IsNullOrEmpty(executableFile))
            {
                throw new InputValidationException("NoScriptPath");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            ClusterAuthenticationCredentials serviceAccount =
                _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id,
                    projectId);
            List<string> commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
                .CreateScheduler(cluster, project)
                .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
                .ToList();

            List<CommandTemplateParameter> templateParameters = new();
            foreach (string parameter in commandTemplateParameters)
            {
                templateParameters.Add(new CommandTemplateParameter()
                {
                    Identifier = parameter,
                    Description = parameter,
                    Query = string.Empty
                });
            }

            CommandTemplate newCommandTemplate = new()
            {
                Name = name,
                Description = description,
                IsGeneric = false,
                IsEnabled = true,
                Project = project,
                ClusterNodeType = commandTemplate.ClusterNodeType,
                ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId,
                ExtendedAllocationCommand = extendedAllocationCommand,
                ExecutableFile = executableFile,
                PreparationScript = preparationScript,
                TemplateParameters = templateParameters,
                CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}")),
                CreatedFromId = commandTemplate.CreatedFromId,
                CreatedFrom = commandTemplate,
                CreatedAt = DateTime.UtcNow
            };

            _logger.Info($"Creating new command template: {newCommandTemplate.Name}");
            _unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
            _unitOfWork.Save();

            return newCommandTemplate;
        }

        public CommandTemplate CreateCommandTemplate(string modelName, string modelDescription,
            string modelExtendedAllocationCommand,
            string modelExecutableFile, string modelPreparationScript, long modelProjectId, long modelClusterNodeTypeId)
        {
            ClusterNodeType clusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(modelClusterNodeTypeId);
            if (clusterNodeType is null)
            {
                throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists");
            }
            
            var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterNodeType.ClusterId.Value,
                modelProjectId);

            if (clusterProject is null || clusterProject.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", modelClusterNodeTypeId, modelProjectId);
            }

            Project project = clusterProject.Project;
            if (project is null || project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }
            
            CommandTemplate commandTemplate = new()
            {
                Name = modelName,
                Description = modelDescription,
                IsGeneric = false,
                IsEnabled = true,
                Project = project,
                ProjectId = project.Id,
                ClusterNodeType = clusterNodeType,
                ClusterNodeTypeId = clusterNodeType.Id,
                ExtendedAllocationCommand = modelExtendedAllocationCommand,
                ExecutableFile = modelExecutableFile,
                PreparationScript = modelPreparationScript,
                TemplateParameters = new List<CommandTemplateParameter>(),
                CommandParameters = string.Empty,
                CreatedAt = DateTime.UtcNow,
                CreatedFrom = null
            };

            _logger.Info($"Creating new command template: {commandTemplate}");
            _unitOfWork.CommandTemplateRepository.Insert(commandTemplate);
            _unitOfWork.Save();

            return commandTemplate;
        }
        
        public CommandTemplate ModifyCommandTemplate(long modelId, string modelName, string modelDescription,
            string modelExtendedAllocationCommand, string modelExecutableFile, string modelPreparationScript,
            long modelClusterNodeTypeId)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(modelId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }
            
            if(commandTemplate.CreatedFrom is not null)
            {
                throw new InvalidRequestException("CommandTemplateNotStatic");
            }

            Project project = commandTemplate.Project;
            if (project is null)
            {
                throw new InvalidRequestException("NotPermitted");
            }
            
            if (project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }

            ClusterNodeType clusterNodeType = _unitOfWork.ClusterNodeTypeRepository.GetById(modelClusterNodeTypeId);
            if (clusterNodeType is null)
            {
                throw new RequestedObjectDoesNotExistException("ClusterNodeTypeNotExists");
            }
            
            var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterNodeType.ClusterId.Value,
                project.Id);
            
            if (clusterProject is null || clusterProject.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ClusterProjectCombinationNotFound", modelClusterNodeTypeId, project.Id);
            }
            
            commandTemplate.Name = modelName;
            commandTemplate.Description = modelDescription;
            commandTemplate.ExtendedAllocationCommand = modelExtendedAllocationCommand;
            commandTemplate.ExecutableFile = modelExecutableFile;
            commandTemplate.PreparationScript = modelPreparationScript;
            commandTemplate.ClusterNodeType = clusterNodeType;
            commandTemplate.ClusterNodeTypeId = clusterNodeType.Id;
            commandTemplate.ModifiedAt = DateTime.UtcNow;
            
            _logger.Info($"Modifying command template: {commandTemplate}");
            _unitOfWork.Save();

            return commandTemplate;
        }

        /// <summary>
        /// Modify command template
        /// </summary>
        /// <param name="commandTemplateId"></param>
        /// <param name="name"></param>
        /// <param name="projectId"></param>
        /// <param name="description"></param>
        /// <param name="extendedAllocationCommand"></param>
        /// <param name="executableFile"></param>
        /// <param name="preparationScript"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="InputValidationException"></exception>
        public CommandTemplate ModifyCommandTemplateFromGeneric(long commandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }
            
            if(commandTemplate.CreatedFrom is null)
            {
                throw new InvalidRequestException("CommandTemplateNotFromGeneric");
            }
            
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null)
            {
                throw new InvalidRequestException("NotPermitted");
            }
            
            if (project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }

            if (commandTemplate.IsGeneric)
            {
                throw new InputValidationException("CommandTemplateIsGeneric");
            }

            if (executableFile is null)
            {
                throw new InputValidationException("CommandTemplateNoExecutableFile");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
            var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                                             .CreateScheduler(cluster, project)
                                                             .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
                                                             .ToList();

            List<CommandTemplateParameter> templateParameters = new();
            foreach (string parameter in commandTemplateParameters)
            {
                templateParameters.Add(new CommandTemplateParameter()
                {
                    Identifier = parameter,
                    Description = parameter,
                    Query = string.Empty
                });
            }

            _logger.Info($"Modifying command template: {commandTemplate.Name}");
            commandTemplate.Name = name;
            commandTemplate.Description = description;
            commandTemplate.ExtendedAllocationCommand = extendedAllocationCommand;
            commandTemplate.PreparationScript = preparationScript;
            commandTemplate.TemplateParameters.ForEach(_unitOfWork.CommandTemplateParameterRepository.Delete);
            commandTemplate.TemplateParameters.AddRange(templateParameters);
            commandTemplate.ExecutableFile = executableFile;
            commandTemplate.CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"));
            commandTemplate.ModifiedAt = DateTime.UtcNow;

            _logger.Info($"Modifying command template: {commandTemplate}");
            _unitOfWork.Save();
            return commandTemplate;
        }

        /// <summary>
        /// Removes the specified command template from the database
        /// </summary>
        /// <param name="commandTemplateId"></param>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public void RemoveCommandTemplate(long commandTemplateId)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId) ?? throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");

            _logger.Info($"Removing command template: {commandTemplate}");
            commandTemplate.IsEnabled = false;
            _unitOfWork.Save();
        }
        /// <summary>
        /// Creates a new project in the database and returns it
        /// </summary>
        /// <param name="accountingString"></param>
        /// <param name="usageType"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="loggedUser"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        public DomainObjects.JobManagement.Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, string piEmail, AdaptorUser loggedUser)
        {
            DomainObjects.JobManagement.Project existingProject = _unitOfWork.ProjectRepository.GetByAccountingString(accountingString);
            if (existingProject != null)
            {
                var errorMessage = existingProject.IsDeleted
                    ? "ProjectDeleted"
                    : "ProjectAlreadyExist";
                throw new InputValidationException(errorMessage);
            }

            Contact contact = _unitOfWork.ContactRepository.GetByEmail(piEmail)
                ?? new Contact()
                {
                    Email = piEmail,
                };

            Project project = InitializeProject(accountingString, usageType, name, description, startDate, endDate, useAccountingStringForScheduler, contact);

            // Create user groups for different purposes
            var defaultAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, string.Empty);
            var lexisAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, LexisAuthenticationConfiguration.HEAppEGroupNamePrefix);
            var openIdAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, ExternalAuthConfiguration.HEAppEUserPrefix);

            using (TransactionScope transactionScope = new(
                        TransactionScopeOption.Required,
                        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                _unitOfWork.AdaptorUserGroupRepository.Insert(defaultAdaptorUserGroup);
                _unitOfWork.AdaptorUserGroupRepository.Insert(lexisAdaptorUserGroup);
                _unitOfWork.AdaptorUserGroupRepository.Insert(openIdAdaptorUserGroup);

                _unitOfWork.Save();

                // Check if an admin user exists and is not the logged-in user
                var heappeAdminUser = _unitOfWork.AdaptorUserRepository.GetById(1);
                if (heappeAdminUser != null && heappeAdminUser.Id != loggedUser.Id)
                {
                    heappeAdminUser.CreateSpecificUserRoleForUser(defaultAdaptorUserGroup, AdaptorUserRoleType.Administrator);
                    _unitOfWork.AdaptorUserRepository.Update(heappeAdminUser);
                    _unitOfWork.Save();
                }

                var adaptorUserGroup = loggedUser.UserType switch
                {
                    AdaptorUserType.Default => defaultAdaptorUserGroup,
                    AdaptorUserType.OpenId => openIdAdaptorUserGroup,
                    AdaptorUserType.Lexis => lexisAdaptorUserGroup,
                    _ => defaultAdaptorUserGroup
                };

                loggedUser.CreateSpecificUserRoleForUser(adaptorUserGroup, AdaptorUserRoleType.Administrator);
                _unitOfWork.AdaptorUserRepository.Update(loggedUser);
                _unitOfWork.Save();

                _logger.Info($"Created project with id {project.Id}.");
                transactionScope.Complete();
            }
            return project;
        }

        /// <summary>
        /// Modifies an existing project in the database and returns it
        /// </summary>
        /// <param name="id"></param>
        /// <param name="usageType"></param>
        /// <param name="modelName"></param>
        /// <param name="description"></param>
        /// <param name="accountingString"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public DomainObjects.JobManagement.Project ModifyProject(long id, UsageType usageType, string modelName, string description, DateTime startDate, DateTime endDate, bool? useAccountingStringForScheduler)
        {
            var project = _unitOfWork.ProjectRepository.GetById(id) ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            if (project is null)
            {
                throw new InvalidRequestException("NotPermitted");
            }
            
            if (project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }

            project.UsageType = usageType;
            project.Name = modelName ?? project.Name;
            project.Description = description ?? project.Description;
            project.StartDate = startDate;
            project.EndDate = endDate;
            project.ModifiedAt = DateTime.UtcNow;
            project.UseAccountingStringForScheduler = useAccountingStringForScheduler ?? project.UseAccountingStringForScheduler;

            _unitOfWork.ProjectRepository.Update(project);
            _unitOfWork.Save();
            _logger.Info($"Project ID '{project.Id}' has been modified.");

            return project;
        }

        /// <summary>
        /// Removes project from the database
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public void RemoveProject(long id)
        {
            var project = _unitOfWork.ProjectRepository.GetById(id) ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            if (project is null)
            {
                throw new InvalidRequestException("NotPermitted");
            }
            
            if (project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }

            project.IsDeleted = true;
            project.ModifiedAt = DateTime.UtcNow;
            project.ClusterProjects.ForEach(x => RemoveProjectAssignmentToCluster(x.ProjectId, x.ClusterId));
            _unitOfWork.ProjectRepository.Update(project);
            _logger.Info($"Project id '{project.Id}' has been deleted.");
            _unitOfWork.Save();
        }

        /// <summary>
        /// Assigns a project to a clusters
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <param name="localBasepath"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="InputValidationException"></exception>
        public ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId) ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            _ = _unitOfWork.ClusterRepository.GetById(clusterId) ?? throw new RequestedObjectDoesNotExistException("ClusterNotExists", clusterId);

            var cp = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
            if (cp != null)
            {
                //Cluster to project is marked as deleted, update it
                return !cp.IsDeleted
                    ? throw new InputValidationException("ProjectAlreadyExistWithCluster")
                    : ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);
            }

            //Create cluster to project mapping
            var modified = DateTime.UtcNow;
            ClusterProject clusterProject = new()
            {
                ClusterId = clusterId,
                ProjectId = projectId,
                LocalBasepath = localBasepath.Replace(_scripts.SubExecutionsPath, string.Empty, true, CultureInfo.InvariantCulture).TrimEnd(new char[] { '\\', '/'}),
                CreatedAt = modified,
                IsDeleted = false,
            };

            project.ModifiedAt = modified;
            _unitOfWork.ProjectRepository.Update(project);
            _unitOfWork.ClusterProjectRepository.Insert(clusterProject);
            _unitOfWork.Save();

            _logger.Info($"Created Project ID '{projectId} assignment to Cluster ID '{clusterId}'.");
            return clusterProject;
        }

        /// <summary>
        /// Modifies a project assignment to a clusters
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <param name="localBasepath"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        public ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
        {
            var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId)
                ?? throw new InputValidationException("ProjectNoReferenceToCluster", projectId, clusterId);

            var modified = DateTime.UtcNow;
            clusterProject.LocalBasepath = localBasepath.Replace(_scripts.SubExecutionsPath, string.Empty, true, CultureInfo.InvariantCulture).TrimEnd(new char[] { '\\', '/' });
            clusterProject.ModifiedAt = modified;
            clusterProject.IsDeleted = false;
            clusterProject.Project.ModifiedAt = modified;
            _unitOfWork.ProjectRepository.Update(clusterProject.Project);
            _unitOfWork.ClusterProjectRepository.Update(clusterProject);
            _unitOfWork.Save();

            _logger.Info($"Project ID '{projectId}' assignment to Cluster ID '{clusterId}' was modified.");
            return clusterProject;
        }

        /// <summary>
        /// Removes a project assignment to a cluster
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <exception cref="InputValidationException"></exception>
        public void RemoveProjectAssignmentToCluster(long projectId, long clusterId)
        {
            ClusterProject clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId)
                ?? throw new InputValidationException("ProjectNoReferenceToCluster", projectId, clusterId);

            var modified = DateTime.UtcNow;
            clusterProject.IsDeleted = true;
            clusterProject.ModifiedAt = modified;
            clusterProject.ClusterProjectCredentials.ForEach(x =>
            {
                x.IsDeleted = true;
                x.ModifiedAt = modified;
                x.ClusterAuthenticationCredentials.IsDeleted = true;
            });
            clusterProject.Project.ModifiedAt = modified;
            _unitOfWork.ProjectRepository.Update(clusterProject.Project);
            _unitOfWork.ClusterProjectRepository.Update(clusterProject);
            _unitOfWork.Save();

            _logger.Info($"Removed assignment of the Project with ID '{projectId}' to the Cluster ID '{clusterId}'");
        }

        /// <summary>
        /// Creates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="InputValidationException"></exception>
        public List<SecureShellKey> CreateSecureShellKey(IEnumerable<(string, string)> credentials, long projectId)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || project.IsDeleted || project.EndDate < DateTime.UtcNow)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }
            List<SecureShellKey> secureShellKeys = new();
            foreach ((string username, string password) in credentials)
            {
                IEnumerable<ClusterAuthenticationCredentials> existingCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsForUsernameAndProject(username, projectId);
                if (existingCredentials.Any())
                {
                    //get existing secure key
                    var existingKey = existingCredentials.FirstOrDefault();
                    if (existingKey != null && string.IsNullOrEmpty(existingKey.PrivateKeyFile))
                    {
                        continue;
                    }
                    else if (existingKey != null)
                    {
                        //get PUBLIC KEY FROM PRIVATE KEY
                        SecureShellKey sshKey = SSHGenerator.GetPublicKeyFromPrivateKey(existingKey);
                        secureShellKeys.Add(sshKey);
                        continue;
                    }
                }

                secureShellKeys.Add(CreateSecureShellKey(username, password, project));
            }

            return secureShellKeys;
        }

        private SecureShellKey CreateSecureShellKey(string username, string password, Project project)
        {
            _logger.Info($"Creating SSH key for user {username} for project {project.Name}.");
            var clusterProjects = _unitOfWork.ClusterProjectRepository.GetAll().Where(x => x.ProjectId == project.Id).ToList();
            if (!clusterProjects.Any())
            {
                throw new InputValidationException("ProjectNoAssignToCluster");
            }

            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);
            string keyPath = GetUniquePrivateKeyPath(project.AccountingString);
            new FileInfo(keyPath).Directory.Create();
            File.WriteAllText(keyPath, secureShellKey.PrivateKeyPEM);

            ClusterAuthenticationCredentials serviceCredentials = CreateClusterAuthenticationCredentials(username, password, keyPath, passphrase, secureShellKey.PublicKeyFingerprint, clusterProjects.FirstOrDefault()?.Cluster);
            ClusterAuthenticationCredentials nonServiceCredentials = CreateClusterAuthenticationCredentials(username, password, keyPath, passphrase, secureShellKey.PublicKeyFingerprint, clusterProjects.FirstOrDefault()?.Cluster);

            foreach (ClusterProject clusterProject in clusterProjects)
            {
                var serviceAccount =
                    _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                        clusterProject.ClusterId, project.Id);

                if (serviceAccount == null)
                {
                    serviceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, serviceCredentials, true));
                    _logger.Info($"Service account not found or deleted. Creating new service account for project {project.Id} on cluster {clusterProject.ClusterId}.");
                }
                nonServiceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, nonServiceCredentials, false));
                _logger.Info($"Creating new SSH key for project {project.Id} on cluster {clusterProject.ClusterId}.");

            }

            project.ModifiedAt = DateTime.UtcNow;
            _unitOfWork.ProjectRepository.Update(project);
            if (serviceCredentials.ClusterProjectCredentials.Any())
            {
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(serviceCredentials);
            }

            _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(nonServiceCredentials);
            _unitOfWork.Save();
            return secureShellKey;
        }

        /// <summary>
        /// Recreates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public SecureShellKey RegenerateSecureShellKey(string username, string password, long projectId)
        {
            IEnumerable<ClusterAuthenticationCredentials> clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(w => w.Username == username && !w.IsDeleted && w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent && w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

            if (!clusterAuthenticationCredentials.Any())
            {
                throw new InvalidRequestException("HPCIdentityNotFound");
            }

            _logger.Info($"Recreating SSH key for user {username}.");

            DateTime modificationDate = DateTime.UtcNow;
            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

            foreach (ClusterAuthenticationCredentials credentials in clusterAuthenticationCredentials)
            {
                File.WriteAllText(credentials.PrivateKeyFile, secureShellKey.PrivateKeyPEM);
                credentials.PrivateKeyPassword = passphrase;
                credentials.PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint;
                credentials.CipherType = secureShellKey.CipherType;
                credentials.ClusterProjectCredentials.ForEach(cpc =>
                {
                    cpc.IsDeleted = false;
                    cpc.ModifiedAt = modificationDate;
                    cpc.ClusterProject.Project.ModifiedAt = modificationDate;
                });
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }

            _unitOfWork.Save();
            return secureShellKey;
        }

        /// <summary>
        /// Removes encrypted SSH key
        /// </summary>
        /// <param name="username"></param>
        /// <param name="projectId"></param>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public void RemoveSecureShellKey(string username, long projectId)
        {
            IEnumerable<ClusterAuthenticationCredentials> clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(w => w.Username == username && !w.IsDeleted && w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent && w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

            if (!clusterAuthenticationCredentials.Any())
            {
                throw new InvalidRequestException("HPCIdentityNotFound");
            }


            DateTime modificationDate = DateTime.UtcNow;
            _logger.Info($"Removing SSH key for user {clusterAuthenticationCredentials.First().Username}.");
            foreach (ClusterAuthenticationCredentials credentials in clusterAuthenticationCredentials)
            {
                File.Delete(credentials.PrivateKeyFile);
                credentials.IsDeleted = true;
                credentials.ClusterProjectCredentials.ForEach(cpc =>
                {
                    cpc.IsDeleted = true;
                    cpc.ModifiedAt = modificationDate;
                    cpc.ClusterProject.Project.ModifiedAt = modificationDate;
                });
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }
            _unitOfWork.Save();
        }

        /// <summary>
        /// Recreates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="password"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        [Obsolete]
        public SecureShellKey RegenerateSecureShellKeyByPublicKey(string publicKey, string password, long projectId)
        {

            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
                .ToList();
            if (!clusterAuthenticationCredentials.Any())
            {
                throw new RequestedObjectDoesNotExistException("PublicKeyNotFound");
            }

            var username = clusterAuthenticationCredentials.First().Username;

            _logger.Info($"Recreating SSH key for user {username}.");

            DateTime modificationDate = DateTime.UtcNow;
            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

            foreach (ClusterAuthenticationCredentials credentials in clusterAuthenticationCredentials)
            {
                File.WriteAllText(credentials.PrivateKeyFile, secureShellKey.PrivateKeyPEM);
                credentials.PrivateKeyPassword = passphrase;
                credentials.PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint;
                credentials.CipherType = secureShellKey.CipherType;
                credentials.ClusterProjectCredentials.ForEach(cpc =>
                {
                    cpc.IsDeleted = false;
                    cpc.ModifiedAt = modificationDate;
                    cpc.ClusterProject.Project.ModifiedAt = modificationDate;
                });
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }

            _unitOfWork.Save();
            return secureShellKey;
        }

        /// <summary>
        /// Removes encrypted SSH key
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="projectId"></param>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        [Obsolete]
        public void RemoveSecureShellKeyByPublicKey(string publicKey, long projectId)
        {
            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
                .ToList();

            if (!clusterAuthenticationCredentials.Any())
            {
                throw new RequestedObjectDoesNotExistException("PublicKeyNotFound");
            }

            DateTime modificationDate = DateTime.UtcNow;
            _logger.Info($"Removing SSH key for user {clusterAuthenticationCredentials.First().Username}.");
            foreach (ClusterAuthenticationCredentials credentials in clusterAuthenticationCredentials)
            {
                File.Delete(credentials.PrivateKeyFile);
                credentials.IsDeleted = true;
                credentials.ClusterProjectCredentials.ForEach(cpc =>
                {
                    cpc.IsDeleted = true;
                    cpc.ModifiedAt = modificationDate;
                    cpc.ClusterProject.Project.ModifiedAt = modificationDate;
                });
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }
            _unitOfWork.Save();
        }

        /// <summary>
        /// Initialize cluster script directory and create symbolic link for user
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterProjectRootDirectory"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public List<ClusterInitReport> InitializeClusterScriptDirectory(long projectId, string clusterProjectRootDirectory)
        {
            clusterProjectRootDirectory = clusterProjectRootDirectory.Replace(_scripts.SubScriptsPath, string.Empty, true, CultureInfo.InvariantCulture).TrimEnd(new char[] { '\\', '/' });
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAuthenticationCredentialsProject(projectId)
                .ToList();
            Dictionary<Cluster, ClusterInitReport> clusterInitReports = new();
            if (!clusterAuthenticationCredentials.Any())
            {
                throw new RequestedObjectDoesNotExistException("NotExistingPublicKey");
            }

            foreach (var clusterAuthCredentials in clusterAuthenticationCredentials.DistinctBy(x => x.Username))
            {
                if (clusterAuthCredentials.IsDeleted)
                {
                    continue;
                }

                foreach (ClusterProjectCredential clusterProjectCredential in clusterAuthCredentials.ClusterProjectCredentials.DistinctBy(x => x.ClusterProject))
                {
                    if (clusterProjectCredential.IsDeleted)
                    {
                        continue;
                    }

                    Cluster cluster = clusterProjectCredential.ClusterProject.Cluster;
                    var project = clusterProjectCredential.ClusterProject.Project;
                    string localBasepath = clusterProjectCredential.ClusterProject.LocalBasepath;
                    HpcConnectionFramework.SchedulerAdapters.Interfaces.IRexScheduler scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project);
                    bool isInitialized = scheduler.InitializeClusterScriptDirectory(clusterProjectRootDirectory, localBasepath, cluster, clusterAuthCredentials, clusterProjectCredential.IsServiceAccount);
                    if (isInitialized)
                    {
                        if (!clusterInitReports.ContainsKey(cluster))
                        {
                            clusterInitReports.Add(cluster, new ClusterInitReport()
                            {
                                Cluster = cluster,
                                NumberOfInitializedAccounts = 1
                            });
                        }
                        else
                        {
                            clusterInitReports[cluster].NumberOfInitializedAccounts++;
                        }
                        _logger.Info($"Initialized cluster script directory for project {project.Id} on cluster {cluster.Id} with account {clusterAuthCredentials.Username}.");
                    }
                    else
                    {
                        if (!clusterInitReports.ContainsKey(cluster))
                        {
                            clusterInitReports.Add(cluster, new ClusterInitReport()
                            {
                                Cluster = cluster,
                                NumberOfNotInitializedAccounts = 1
                            });
                        }
                        else
                        {
                            clusterInitReports[cluster].NumberOfNotInitializedAccounts++;
                        }
                        _logger.Error($"Initialization of cluster script directory failed for project {project.Id} on cluster {cluster.Id} with account {clusterProjectCredential.ClusterAuthenticationCredentials.Username}.");
                    }
                }
            }

            return clusterInitReports.Values.ToList();
        }

        /// <summary>
        /// Test cluster access for robot account
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public bool TestClusterAccessForAccount(long projectId, string username)
        {

            IEnumerable<ClusterAuthenticationCredentials> clusterAuthenticationCredentials = string.IsNullOrEmpty(username)
                ? _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGenerated(projectId).ToList()
                : _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Where(w => w.Username == username && !w.IsDeleted && w.AuthenticationType != ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent && w.ClusterProjectCredentials.Any(a => a.ClusterProject.ProjectId == projectId));

            if (!clusterAuthenticationCredentials.Any())
            {
                throw new InvalidRequestException("HPCIdentityNotFound");
            }

            List<long> noAccessClusterIds = new();
            foreach (ClusterAuthenticationCredentials clusterAuthCredentials in clusterAuthenticationCredentials.DistinctBy(x => x.Username).Where(x=>!x.IsDeleted))
            {
                if (clusterAuthCredentials.IsDeleted)
                {
                    continue;
                }
                foreach (ClusterProjectCredential clusterProjectCredential in clusterAuthCredentials.ClusterProjectCredentials.DistinctBy(x => x.ClusterProject).Where(x=>!x.IsDeleted))
                {
                    if (clusterAuthCredentials.IsDeleted || clusterProjectCredential.IsDeleted || clusterProjectCredential.ClusterProject.IsDeleted)
                    {
                        continue;
                    }

                    Cluster cluster = clusterProjectCredential.ClusterProject.Cluster;
                    var project = clusterProjectCredential.ClusterProject.Project;
                    
                    HpcConnectionFramework.SchedulerAdapters.Interfaces.IRexScheduler scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, project);
                    if (!scheduler.TestClusterAccessForAccount(cluster, clusterAuthCredentials))
                    {
                        noAccessClusterIds.Add(cluster.Id);
                    }
                }
            }

            return !noAccessClusterIds.Any();
        }

        public CommandTemplateParameter CreateCommandTemplateParameter(string modelIdentifier, string modelQuery,
            string modelDescription, long modelCommandTemplateId)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(modelCommandTemplateId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }
            
            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }
            
            //if is not static
            if (commandTemplate.CreatedFrom is not null)
            {
                throw new InvalidRequestException("CommandTemplateNotStatic");
            }
            
            //if identifier already exists in command template
            if (commandTemplate.TemplateParameters.Exists(x => x.Identifier == modelIdentifier))
            {
                throw new InputValidationException("CommandTemplateAlreadyParameterExists");
            }

            CommandTemplateParameter commandTemplateParameter = new()
            {
                Identifier = modelIdentifier,
                Query = modelQuery,
                Description = modelDescription,
                CommandTemplate = commandTemplate,
                CommandTemplateId = commandTemplate.Id,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.CommandTemplateParameterRepository.Insert(commandTemplateParameter);
            AddCommandTemplateParameterToCommandTemplate(commandTemplate, commandTemplateParameter);
            _unitOfWork.Save();

            return commandTemplateParameter;
        }

        public CommandTemplateParameter ModifyCommandTemplateParameter(long modelId, string modelIdentifier, string modelQuery,
            string modelDescription)
        {
            CommandTemplateParameter commandTemplateParameter = _unitOfWork.CommandTemplateParameterRepository.GetById(modelId);
            if (commandTemplateParameter is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound");
            }
            
            if (!commandTemplateParameter.CommandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }
            
            //if is not static
            if (commandTemplateParameter.CommandTemplate.CreatedFrom is not null)
            {
                throw new InvalidRequestException("CommandTemplateNotStatic");
            }
            
            //if identifier already exists in command template
            if (!commandTemplateParameter.CommandTemplate.TemplateParameters.Exists(x => x.Identifier == modelIdentifier))
            {
                string previousIdentifier = commandTemplateParameter.Identifier;
                commandTemplateParameter.Identifier = modelIdentifier;
                ModifyCommandTemplateParameterFromCommandTemplate(commandTemplateParameter.CommandTemplate, commandTemplateParameter, previousIdentifier);
            }
            
            commandTemplateParameter.Query = modelQuery;
            commandTemplateParameter.Description = modelDescription;
            commandTemplateParameter.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.CommandTemplateParameterRepository.Update(commandTemplateParameter);
            _unitOfWork.Save();

            return commandTemplateParameter;
        }

        public void RemoveCommandTemplateParameter(long modelId)
        {
            CommandTemplateParameter commandTemplateParameter = _unitOfWork.CommandTemplateParameterRepository.GetById(modelId);
            if (commandTemplateParameter is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateParameterNotFound");
            }
            
            if (!commandTemplateParameter.CommandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }
            
            //if is not static
            if (commandTemplateParameter.CommandTemplate.CreatedFrom is not null)
            {
                throw new InvalidRequestException("CommandTemplateNotStatic");
            }

            RemoveCommandTemplateParameterFromCommandTemplate(commandTemplateParameter.CommandTemplate, commandTemplateParameter);
            commandTemplateParameter.IsEnabled = false;
            commandTemplateParameter.ModifiedAt = DateTime.UtcNow;
            commandTemplateParameter.IsVisible = false;
            _unitOfWork.Save();
        }

        public List<CommandTemplate> ListCommandTemplates(long projectId)
        {
            return _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(projectId)
                .Where(x => x.IsEnabled)
                .Select(template => new CommandTemplate
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    ExtendedAllocationCommand = template.ExtendedAllocationCommand,
                    PreparationScript = template.PreparationScript,
                    ExecutableFile = template.ExecutableFile,
                    CommandParameters = template.CommandParameters,
                    IsEnabled = template.IsEnabled,
                    IsGeneric = template.IsGeneric,
                    CreatedAt = template.CreatedAt,
                    ModifiedAt = template.ModifiedAt,
                    CreatedFrom = template.CreatedFrom,
                    ClusterNodeType = template.ClusterNodeType,
                    TemplateParameters = template.TemplateParameters.Where(x => x.IsEnabled).ToList()
                })
                .ToList();
        }
        
        #region SubProject
        /// <summary>
        /// Creates a new subproject if it does not exist
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public SubProject CreateSubProject(string identifier, long projectId)
        {
            SubProject subProject = _unitOfWork.SubProjectRepository.GetByIdentifier(identifier, projectId);
            if (subProject is not null && (subProject.IsDeleted || subProject.EndDate <= DateTime.UtcNow || subProject.StartDate >= DateTime.UtcNow))
            {
                throw new InputValidationException("SubProjectDeletedOrEnded");
            }
            else if(subProject is not null)
            {
                //already exists, reuse it
                return subProject;
            }
            else
            {
                //create new 
                SubProject newSubProject = new()
                {
                    Identifier = identifier,
                    CreatedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow,
                    IsDeleted = false,
                    ProjectId = projectId
                };
                _unitOfWork.SubProjectRepository.Insert(newSubProject);
                _unitOfWork.Save();
                return newSubProject;
            }
        }

        public SubProject CreateSubProject(long modelProjectId, string modelIdentifier, string modelDescription,
            DateTime modelStartDate, DateTime? modelEndDate)
        {
            //test if not exist subproject with the same identifier
            if (_unitOfWork.SubProjectRepository.GetByIdentifier(modelIdentifier, modelProjectId) != null)
            {
                throw new InputValidationException("SubProjectIdentifierAlreadyExists");
            }
            SubProject newSubProject = new()
            {
                Identifier = modelIdentifier,
                Description = modelDescription,
                CreatedAt = DateTime.UtcNow,
                StartDate = modelStartDate,
                EndDate = modelEndDate,
                IsDeleted = false,
                ProjectId = modelProjectId
            };
            _unitOfWork.SubProjectRepository.Insert(newSubProject);
            _unitOfWork.Save();
            return newSubProject;
        }

        public SubProject ModifySubProject(long modelId, string modelIdentifier, string modelDescription, DateTime modelStartDate,
            DateTime? modelEndDate)
        {
            SubProject subProject = _unitOfWork.SubProjectRepository.GetById(modelId)
                                    ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
            if (!subProject.IsDeleted)
            {
                throw new InputValidationException("NotPermitted");
            }
            var subProjectWithSameIdentifier = _unitOfWork.SubProjectRepository.GetByIdentifier(modelIdentifier, subProject.ProjectId);
            if (subProjectWithSameIdentifier != null && subProjectWithSameIdentifier.Id != modelId)
            {
                throw new InputValidationException("SubProjectIdentifierAlreadyExists");
            }
            subProject.Identifier = modelIdentifier;
            subProject.Description = modelDescription;
            subProject.StartDate = modelStartDate;
            subProject.EndDate = modelEndDate;
            subProject.ModifiedAt = DateTime.UtcNow;
            _unitOfWork.SubProjectRepository.Update(subProject);
            _unitOfWork.Save();
            return subProject;
        }

        public void RemoveSubProject(long modelId)
        {
            SubProject subProject = _unitOfWork.SubProjectRepository.GetById(modelId)
                                    ?? throw new RequestedObjectDoesNotExistException("SubProjectNotFound");
            if (!subProject.IsDeleted)
            {
                throw new InputValidationException("NotPermitted");
            }
            subProject.IsDeleted = true;
            subProject.ModifiedAt = DateTime.UtcNow;
            _unitOfWork.SubProjectRepository.Update(subProject);
            _unitOfWork.Save();
        }

        public void ComputeAccounting(DateTime modelStartTime, DateTime modelEndTime, long projectId)
        {
            //get all submittedtasks from project and compute with formula
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }
            if (project.IsDeleted)
            {
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");
            }
            
            var submittedTasks = _unitOfWork.SubmittedTaskInfoRepository
                .GetAll()
                .Where(t=>t.StartTime >= modelStartTime 
                          && t.EndTime <= modelEndTime 
                          && t.Project.Id == projectId)
                .ToList();
            
            //compute accounting
            foreach (var submittedTask in submittedTasks)
            {
                //compute accounting
                string accountingFormula = submittedTask
                    .Specification
                    .ClusterNodeType
                    .ClusterNodeTypeAggregation
                    .ClusterNodeTypeAggregationAccountings
                    .LastOrDefault(x=>!(x.Accounting.IsDeleted) && x.Accounting.IsValid(submittedTask.StartTime, submittedTask.EndTime))
                    ?.Accounting.Formula;
                
                //parse all parameters to dictionary
                var parsedParameters = submittedTask.AllParameters
                    .Split(' ')
                    .Select(x => x.Split('='))
                    .ToDictionary(x => x[0], x => x.Length >= 2 ? x[1] : string.Empty);

                double result = ResourceAccountingUtils.CalculateAllocatedResources(accountingFormula, parsedParameters, _logger); 
                submittedTask.ResourceConsumed = result;
                _unitOfWork.SubmittedTaskInfoRepository.Update(submittedTask);
                _unitOfWork.Save();
            }
        }

        #endregion

        #region Private methods
        private void AddCommandTemplateParameterToCommandTemplate(CommandTemplate commandTemplate, CommandTemplateParameter commandTemplateParameter)
        {
            //if commandTemplate.CommandParameters does not contain commandTemplateParameter.Identifier
            if (!commandTemplate.CommandParameters.Contains(commandTemplateParameter.Identifier))
            {
                commandTemplate.CommandParameters = string.Join(' ', commandTemplate.TemplateParameters.Select(x => $"%%{"{"}{x.Identifier}{"}"}"));
            }
        }

        private void ModifyCommandTemplateParameterFromCommandTemplate(CommandTemplate commandTemplate,
            CommandTemplateParameter commandTemplateParameter, string previousIdentifier)
        {
            // modify commandTemplate.CommandParameters, replace %%{previousIdentifier} with %%{commandTemplateParameter.Identifier}
            commandTemplate.CommandParameters = commandTemplate.CommandParameters.Replace($"%%{"{"}{previousIdentifier}{"}"}", $"%%{"{"}{commandTemplateParameter.Identifier}{"}"}");
        }
        
        private void RemoveCommandTemplateParameterFromCommandTemplate(CommandTemplate commandTemplate, CommandTemplateParameter commandTemplateParameter)
        {
            commandTemplate.TemplateParameters.Remove(commandTemplateParameter);
            commandTemplate.CommandParameters = string.Join(' ', commandTemplate.TemplateParameters.Select(x => $"%%{"{"}{x.Identifier}{"}"}"));
        }
        
        /// <summary>
        /// Returns the path to the private key file for the specified project
        /// </summary>
        /// <param name="accountingString"></param>
        /// <returns></returns>
        private string GetUniquePrivateKeyPath(string accountingString)
        {
            string directoryPath = Path.Combine(CertificateGeneratorConfiguration.GeneratedKeysDirectory, accountingString);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            //get count of files in directory and increment by 1
            int nextId =  Directory.GetFiles(directoryPath).Length + 1;
            string keyPath = Path.Combine(directoryPath, $"{CertificateGeneratorConfiguration.GeneratedKeyPrefix}_{nextId:D2}");
            return keyPath;
        }

        /// <summary>
        /// Initializes a new project with the specified parameters
        /// </summary>
        /// <param name="accountingString"></param>
        /// <param name="usageType"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="useAccountingStringForScheduler"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
        private static Project InitializeProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, bool useAccountingStringForScheduler, Contact contact)
        {
            return new Project
            {
                AccountingString = accountingString,
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                UsageType = usageType,
                IsDeleted = false,
                UseAccountingStringForScheduler = useAccountingStringForScheduler,
                ProjectContacts = new List<ProjectContact>()
                {
                    new()
                    {
                        IsPI = true,
                        Contact = contact
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new user group with the specified parameters
        /// </summary>
        /// <param name="project"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="prefix"></param>"
        /// <returns></returns>
        private static AdaptorUserGroup CreateAdaptorUserGroup(Project project, string name, string description, string prefix)
        {
            return new AdaptorUserGroup
            {
                Name = string.Concat(prefix, name),
                AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>(),
                Description = string.Concat(prefix, description),
                Project = project
            };
        }

        /// <summary>
        /// Create auth credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="keyPath"></param>
        /// <param name="passphrase"></param>
        /// <param name="publicKeyFingerprint"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private static ClusterAuthenticationCredentials CreateClusterAuthenticationCredentials(string username, string password, string keyPath, string passphrase, string publicKeyFingerprint, Cluster cluster)
        {
            ClusterAuthenticationCredentials credentials = new()
            {
                Username = username,
                Password = password,
                PrivateKeyFile = keyPath,
                PrivateKeyPassword = passphrase,
                CipherType = CipherGeneratorConfiguration.Type,
                PublicKeyFingerprint = publicKeyFingerprint,
                ClusterProjectCredentials = new List<ClusterProjectCredential>(),
                IsGenerated = true
            };
            credentials.AuthenticationType = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credentials, cluster);
            return credentials;
        }

        /// <summary>
        /// Creates a new cluster reference to project and map credentials with the specified parameters
        /// </summary>
        /// <param name="clusterProject"></param>
        /// <param name="clusterAuthenticationCredentials"></param>
        /// <param name="isServiceAccount"></param>
        /// <returns></returns>
        private static ClusterProjectCredential CreateClusterProjectCredentials(ClusterProject clusterProject, ClusterAuthenticationCredentials clusterAuthenticationCredentials, bool isServiceAccount)
        {
            return new ClusterProjectCredential
            {
                ClusterProject = clusterProject,
                ClusterAuthenticationCredentials = clusterAuthenticationCredentials,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsServiceAccount = isServiceAccount
            };
        }

        /// <summary>
        /// Computes the fingerprint of the specified public key in base64 format
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns>SHA256 hash</returns>
        /// <exception cref="InputValidationException"></exception>
        private static string ComputePublicKeyFingerprint(string publicKey)
        {
            publicKey = publicKey.Replace("\n", "");
            Regex base64Regex = new(@"([A-Za-z0-9+\/=]+=)");
            Match base64Match = base64Regex.Match(publicKey);

            if (!base64Match.Success || !TryFromBase64String(base64Match.Value, out byte[] base64EncodedBytes))
            {
                throw new InputValidationException("InvalidPublicKey");
            }

            byte[] fingerprintBytes = DigestUtilities.CalculateDigest("SHA256", base64EncodedBytes);
            return BitConverter.ToString(fingerprintBytes).Replace("-", string.Empty).ToLower();
        }

        private static bool TryFromBase64String(string base64, out byte[] result)
        {
            try
            {
                result = Convert.FromBase64String(base64);
                return true;
            }
            catch (FormatException)
            {
                result = null;
                return false;
            }
        }
        #endregion
    }
}
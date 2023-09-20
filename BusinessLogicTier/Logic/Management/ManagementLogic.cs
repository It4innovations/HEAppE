using HEAppE.BusinessLogicTier.Logic.Management.Exceptions;
using HEAppE.CertificateGenerator;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier.Migrations;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using log4net;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Transactions;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public class ManagementLogic : IManagementLogic
    {
        protected IUnitOfWork _unitOfWork;
        protected static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected string _sshKeysDirectory = "/opt/heappe/keys/";
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
        public CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(genericCommandTemplateId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || !project.IsDeleted)
            {
                _logger.Error($"The specified project with ID '{projectId}' is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException($"The specified project with ID '{projectId}' is not defined in HEAppE!");
            }
            
            if (commandTemplate is null)
            {
                _logger.Error($"The specified command template with id {genericCommandTemplateId} is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }



            if (!commandTemplate.IsGeneric)
            {
                _logger.Error($"The specified command template with id {genericCommandTemplateId} is not generic.");
                throw new InputValidationException("The specified command template is not generic.");
            }

            if (!commandTemplate.IsEnabled)
            {
                _logger.Error($"The specified command template with id {genericCommandTemplateId} is disabled.");
                throw new InputValidationException("The specified command template is deleted.");
            }

            var commandTemplateParameter = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                               .FirstOrDefault();

            if (string.IsNullOrEmpty(commandTemplateParameter?.Identifier))
            {
                _logger.Error($"The user-script command parameter for the generic command template is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException("The user-script command parameter for the generic command template is not defined in HEAppE!");
            }

            if (string.IsNullOrEmpty(executableFile))
            {
                _logger.Error($"The generic command template should contain script path!");
                throw new InputValidationException("The generic command template should contain script path!");
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

            CommandTemplate newCommandTemplate = new CommandTemplate()
            {
                Name = name,
                Description = description,
                IsGeneric = false,
                IsEnabled = true,
                ClusterNodeType = commandTemplate.ClusterNodeType,
                ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId,
                ExtendedAllocationCommand = extendedAllocationCommand,
                ExecutableFile = executableFile,
                PreparationScript = preparationScript,
                TemplateParameters = templateParameters,
                CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"))
            };
            _logger.Info($"Creating new command template: {newCommandTemplate.Name}");
            _unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
            _unitOfWork.Save();

            return newCommandTemplate;
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
        public CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            Project project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || !project.IsDeleted)
            {
                _logger.Error($"The specified project with ID '{projectId}' is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException($"The specified project with ID '{projectId}' is not defined in HEAppE!");
            }
            
            if (commandTemplate is null)
            {
                _logger.Error($"The specified command template with id {commandTemplateId} is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (!commandTemplate.IsEnabled)
            {
                _logger.Error($"The specified command template with id {commandTemplateId} is disabled.");
                throw new InputValidationException("The specified command template is deleted.");
            }

            if (commandTemplate.IsGeneric)
            {
                _logger.Error($"The specified command template with id {commandTemplateId} is generic.");
                throw new InputValidationException("The specified command template is generic.");
            }

            if (executableFile is null)
            {
                _logger.Error($"The specified command template must have specified executable file!");
                throw new InputValidationException("The specified command template must have specified executable file!");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
            var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                                             .CreateScheduler(cluster, project)
                                                             .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
                                                             .ToList();

            var templateParameters = new List<CommandTemplateParameter>();
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
            commandTemplate.TemplateParameters.ForEach(cmdParameters => _unitOfWork.CommandTemplateParameterRepository.Delete(cmdParameters));
            commandTemplate.TemplateParameters.AddRange(templateParameters);
            commandTemplate.ExecutableFile = executableFile;
            commandTemplate.CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"));

            _unitOfWork.Save();
            return commandTemplate;
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
        /// <param name="primaryInvestigatorContactEmail"></param>
        /// <param name="primaryInvestigatorContactPublicKey"></param>
        /// <param name="loggedUser"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Project CreateProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate, AdaptorUser loggedUser)
        {
            // Check if a project with the same accounting string already exists
            var existingProject = _unitOfWork.ProjectRepository.GetByAccountingString(accountingString);
            if (existingProject != null && existingProject.IsDeleted)
            {
                var errorMessage = $"Project with accounting string {accountingString} was previously present in the system and was deleted!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }
            else if (existingProject != null && !existingProject.IsDeleted)
            {
                var errorMessage = $"Project with accounting string {accountingString} already exists!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }

            var project = InitializeProject(accountingString, usageType, name, description, startDate, endDate);

            // Create user groups for different purposes
            var defaultAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, string.Empty);
            var lexisAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, LexisAuthenticationConfiguration.HEAppEGroupNamePrefix);
            var openIdAdaptorUserGroup = CreateAdaptorUserGroup(project, name, description, ExternalAuthConfiguration.HEAppEUserPrefix);

            try
            {
                using (var transactionScope = new TransactionScope(
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
                        var adminUserRole = CreateAdminUserRole(heappeAdminUser, defaultAdaptorUserGroup);
                        heappeAdminUser.AdaptorUserUserGroupRoles.AddRange(adminUserRole);

                        _unitOfWork.AdaptorUserRepository.Update(heappeAdminUser);
                        _unitOfWork.Save();
                    }

                    // Check if the logged-in user has a password
                    if (string.IsNullOrEmpty(loggedUser.Password))
                    {
                        // For externally authenticated users, create admin roles in respective user groups
                        var lexisAdminUserRole = CreateAdminUserRole(loggedUser, lexisAdaptorUserGroup);
                        var openIdAdminUserRole = CreateAdminUserRole(loggedUser, openIdAdaptorUserGroup);

                        loggedUser.AdaptorUserUserGroupRoles.AddRange(lexisAdminUserRole);
                        loggedUser.AdaptorUserUserGroupRoles.AddRange(openIdAdminUserRole);
                    }
                    else
                    {
                        var adminUserRole = CreateAdminUserRole(loggedUser, defaultAdaptorUserGroup);
                        loggedUser.AdaptorUserUserGroupRoles.AddRange(adminUserRole);
                    }

                    // Update the logged-in user
                    _unitOfWork.AdaptorUserRepository.Update(loggedUser);
                    _unitOfWork.Save();

                    _logger.Info($"Created project with id {project.Id}.");
                    transactionScope.Complete();
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while creating project and adaptorUserGroup: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return project;
        }

        private List<AdaptorUserUserGroupRole> CreateAdminUserRole(AdaptorUser user, AdaptorUserGroup group)
        {
            var role = new AdaptorUserUserGroupRole
            {
                AdaptorUserId = user.Id,
                AdaptorUserGroupId = group.Id,
                AdaptorUserRoleId = (long)UserRoleType.Administrator
            };
            var allRoles = _unitOfWork.AdaptorUserRoleRepository.GetAll();
            return UserRoleUtils.GetAllUserRoles(new List<AdaptorUserUserGroupRole> { role }, allRoles).ToList();
        }


        /// <summary>
        /// Modifies an existing project in the database and returns it
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accountingString"></param>
        /// <param name="usageType"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="Exception"></exception>
        public Project ModifyProject(long id, UsageType usageType, string description, DateTime startDate, DateTime endDate)
        {
            var project = _unitOfWork.ProjectRepository.GetById(id);
            if (project == null)
            {
                var errorMessage = $"Project with id {id} does not exist!";
                _logger.Error(errorMessage);
                throw new RequestedObjectDoesNotExistException(errorMessage);
            }

            project.UsageType = usageType;
            project.Description = description ?? project.Description;
            project.StartDate = startDate;
            project.EndDate = endDate;
            project.ModifiedAt = DateTime.UtcNow;

            try
            {
                _unitOfWork.ProjectRepository.Update(project);
                _unitOfWork.Save();
                _logger.Info($"Project ID '{project.Id}' has been modified.");
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while updating project: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return project;
        }

        /// <summary>
        /// Removes project from the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        /// <exception cref="Exception"></exception>
        public string RemoveProject(long id)
        {
            var project = _unitOfWork.ProjectRepository.GetById(id);
            if (project == null)
            {
                var errorMessage = $"Project with id {id} does not exist!";
                _logger.Error(errorMessage);
                throw new RequestedObjectDoesNotExistException(errorMessage);
            }

            try
            {
                project.IsDeleted = true;
                project.ModifiedAt = DateTime.UtcNow;
                project.ClusterProjects.ForEach(x => RemoveProjectAssignmentToCluster(x.ProjectId, x.ClusterId));
                _unitOfWork.ProjectRepository.Update(project);
                _logger.Info($"Project id '{project.Id}' has been deleted.");
                _unitOfWork.Save();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while deleting project: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return $"Project id '{project.Id}' has been deleted";
        }

        /// <summary>
        /// Assings a project to a clusters
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <param name="localBasepath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ClusterProject CreateProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project == null)
            {
                var errorMessage = $"Project with id {projectId} does not exist!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }
            var clusters = _unitOfWork.ClusterRepository.GetById(clusterId);
            if (clusters == null)
            {
                var errorMessage = $"Cluster with id {clusterId} does not exist!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }

            try
            {
                var cp = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
                if (cp != null)
                {
                    //Cluster to project is marked as deleted, update it
                    if (cp.IsDeleted)
                    {
                        return ModifyProjectAssignmentToCluster(projectId, clusterId, localBasepath);
                    }
                    else
                    {
                        var errorMessage = $"Project with id {projectId} is already assigned to cluster with id {clusterId}!";
                        _logger.Error(errorMessage);
                        throw new InputValidationException(errorMessage);
                    }
                }
                else
                {
                    //Create cluster to project mapping
                    var clusterProject = new ClusterProject
                    {
                        ClusterId = clusterId,
                        ProjectId = projectId,
                        LocalBasepath = localBasepath,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                    };
                    project.ModifiedAt = DateTime.UtcNow;
                    _unitOfWork.ProjectRepository.Update(project);
                    _unitOfWork.ClusterProjectRepository.Insert(clusterProject);
                    _unitOfWork.Save();
                    _logger.Info($"Created Project ID '{projectId} assignment to Cluster ID '{clusterId}'.");
                    return clusterProject;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while assigning project to clusters: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Modifies a project assignment to a clusters
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <param name="localBasepath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ClusterProject ModifyProjectAssignmentToCluster(long projectId, long clusterId, string localBasepath)
        {
            try
            {
                var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
                if (clusterProject == null)
                {
                    var errorMessage = $"Project with id {projectId} is not assigned to cluster with id {clusterId}!";
                    _logger.Error(errorMessage);
                    throw new InputValidationException(errorMessage);
                }
                else
                {
                    clusterProject.LocalBasepath = localBasepath;
                    clusterProject.ModifiedAt = DateTime.Now;
                    clusterProject.IsDeleted = false;
                    clusterProject.Project.ModifiedAt = DateTime.UtcNow;
                    _unitOfWork.ProjectRepository.Update(clusterProject.Project);
                    _unitOfWork.ClusterProjectRepository.Update(clusterProject);
                    _unitOfWork.Save();
                    _logger.Info($"Project ID '{projectId}' assignment to Cluster ID '{clusterId}' was modified.");
                    return clusterProject;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while modifying assignment project to cluster: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Removes a project assignment to a cluster
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="clusterId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string RemoveProjectAssignmentToCluster(long projectId, long clusterId)
        {
            try
            {
                var clusterProject = _unitOfWork.ClusterProjectRepository.GetClusterProjectForClusterAndProject(clusterId, projectId);
                if (clusterProject == null)
                {
                    var errorMessage = $"Project with id {projectId} is not assigned to cluster with id {clusterId}!";
                    _logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }
                else
                {
                    clusterProject.IsDeleted = true;
                    clusterProject.ModifiedAt = DateTime.Now;
                    clusterProject.ClusterProjectCredentials.ForEach(x =>
                    {
                        x.IsDeleted = true;
                        x.ModifiedAt = DateTime.UtcNow;
                        x.ClusterAuthenticationCredentials.IsDeleted = true;
                    });
                    clusterProject.Project.ModifiedAt = DateTime.UtcNow;
                    _unitOfWork.ProjectRepository.Update(clusterProject.Project);
                    _unitOfWork.ClusterProjectRepository.Update(clusterProject);
                    _unitOfWork.Save();
                    _logger.Info($"Removed assignment of the Project with ID '{projectId}' to the Cluster ID '{clusterId}'");
                    return $"Removed assignment of the Project with ID '{projectId}' to the Cluster ID '{clusterId}'";
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while removing assignment project to cluster: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Removes the specified command template from the database
        /// </summary>
        /// <param name="commandTemplateId"></param>
        /// <exception cref="RequestedObjectDoesNotExistException"></exception>
        public void RemoveCommandTemplate(long commandTemplateId)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate == null)
            {
                _logger.Error($"The specified command template with id {commandTemplateId} is not defined in HEAppE!");
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }
            _logger.Info($"Removing command template: {commandTemplate.Name}");
            commandTemplate.IsEnabled = false;
            _unitOfWork.Save();
        }

        /// <summary>
        /// Creates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public SecureShellKey CreateSecureShellKey(string username, string password, long projectId)
        {
            var project = _unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || !project.IsDeleted)
            {
                var errorMessage = $"Project with id {projectId} does not exist!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }

            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);
            string keyPath = GetUniquePrivateKeyPath(project.AccountingString);
            //save private key to file
            FileInfo file = new FileInfo(keyPath);
            file.Directory.Create();
            File.WriteAllText(keyPath, secureShellKey.PrivateKeyPEM);

            _logger.Info($"Creating SSH key for user {username} for project {project.Name}.");
            var clusterProjects = _unitOfWork.ClusterProjectRepository.GetAll().Where(x => x.ProjectId == project.Id).ToList();

            ClusterAuthenticationCredentials serviceCredentials = CreateClusterAuthenticationCredentials(username, password, keyPath, passphrase, secureShellKey.PublicKeyFingerprint);
            ClusterAuthenticationCredentials nonServiceCredentials = CreateClusterAuthenticationCredentials(username, password, keyPath, passphrase, secureShellKey.PublicKeyFingerprint);

            foreach (var clusterProject in clusterProjects)
            {
                serviceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, serviceCredentials, true));
                nonServiceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, nonServiceCredentials, false));
            }
            
            project.ModifiedAt = DateTime.UtcNow;
            _unitOfWork.ProjectRepository.Update(project);
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(serviceCredentials);
            _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(nonServiceCredentials);

            _unitOfWork.Save();



            return secureShellKey;
        }

        /// <summary>
        /// Recreates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        public SecureShellKey RecreateSecureShellKey(string username, string password, string publicKey, long projectId)
        {
            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
                                                                                                            .ToList();
            if (clusterAuthenticationCredentials.Count == 0)
            {
                throw new InputValidationException("The specified public key is not defined in HEAppE!");
            }
            _logger.Info($"Recreating SSH key for user {username}.");
            SSHGenerator sshGenerator = new SSHGenerator();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);

            foreach (var credentials in clusterAuthenticationCredentials)
            {
                //overwrite private key file
                File.WriteAllText(credentials.PrivateKeyFile, secureShellKey.PrivateKeyPEM);

                credentials.PrivateKeyPassword = passphrase;
                credentials.PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint;
                credentials.CipherType = secureShellKey.CipherType;
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }

            _unitOfWork.Save();

            return secureShellKey;
        }


        /// <summary>
        /// Removes encrypted SSH key
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        public string RemoveSecureShellKey(string publicKey, long projectId)
        {
            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint, projectId)
                                                                                                             .ToList();

            if (clusterAuthenticationCredentials.Count == 0)
            {
                throw new InputValidationException("The specified public key is not defined in HEAppE!");
            }
            _logger.Info($"Removing SSH key for user {clusterAuthenticationCredentials.First().Username}.");
            foreach (var credentials in clusterAuthenticationCredentials)
            {
                File.Delete(credentials.PrivateKeyFile);
                credentials.IsDeleted = true;
                credentials.ClusterProjectCredentials.ForEach(cpc =>
                {
                     cpc.IsDeleted = true;
                     cpc.ModifiedAt = DateTime.UtcNow;
                     cpc.ClusterProject.Project.ModifiedAt = DateTime.UtcNow;
                });
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Update(credentials);
            }
            _unitOfWork.Save();
            return "SecureShellKey revoked";
        }

        /// <summary>
        /// Computes the fingerprint of the specified public key in base64 format
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns>SHA256 hash</returns>
        /// <exception cref="InputValidationException"></exception>
        private string ComputePublicKeyFingerprint(string publicKey)
        {
            publicKey = publicKey.Replace("\n", "");
            Regex regex = new Regex(@"([A-Za-z0-9+\/=]+=)");
            Match match = regex.Match(publicKey);
            if (!match.Success)
            {
                _logger.Error("The specified public key is not int the valid format!");
                throw new InputValidationException("The specified public key is not valid!");
            }
            else
            {
                var base64EncodedBytes = Convert.FromBase64String(match.Value);
                byte[] fingerprintBytes;

                fingerprintBytes = DigestUtilities.CalculateDigest("SHA256", base64EncodedBytes);
                return BitConverter.ToString(fingerprintBytes).Replace("-", string.Empty).ToLower();
            }
        }

        #region Private methods
        /// <summary>
        /// Returns the path to the private key file for the specified project
        /// </summary>
        /// <param name="projectIds"></param>
        /// <param name="accountingString"></param>
        /// <returns></returns>
        private string GetUniquePrivateKeyPath(string accountingString)
        {
            long netxId = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Max(x => x.Id) + 1;
            string directoryPath = Path.Combine(_sshKeysDirectory, accountingString);
            string keyPath = Path.Combine(directoryPath, $"KEY_{accountingString}_{netxId}");
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
        /// <param name="primaryInvestigatorContactEmail"></param>
        /// <param name="primaryInvestigatorContactPublicKey"></param>
        /// <returns></returns>
        private Project InitializeProject(string accountingString, UsageType usageType, string name, string description, DateTime startDate, DateTime endDate)
        {
            return new Project
            {
                AccountingString = accountingString,
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                StartDate = startDate,
                EndDate = endDate,
                UsageType = usageType,
                IsDeleted = false,
                UseAccountingStringForScheduler = true
            };
        }

        /// <summary>
        /// Creates a new project contact with the specified parameters
        /// </summary>
        /// <param name="primaryInvestigatorContactEmail"></param>
        /// <param name="primaryInvestigatorContactPublicKey"></param>
        /// <returns></returns>
        private List<ProjectContact> CreateProjectContact(string primaryInvestigatorContactEmail, string primaryInvestigatorContactPublicKey)
        {
            var projectContacts = new List<ProjectContact>();

            if (primaryInvestigatorContactEmail is not null)
            {
                var piContact = new ProjectContact
                {
                    IsPI = true,
                    Contact = new Contact
                    {
                        Email = primaryInvestigatorContactEmail,
                        PublicKey = primaryInvestigatorContactPublicKey,
                        IsDeleted = false,
                        CreatedAt = DateTime.Now
                    }
                };
                projectContacts.Add(piContact);
            }

            return projectContacts;
        }

        /// <summary>
        /// Creates a new user group with the specified parameters
        /// </summary>
        /// <param name="project"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="prefix"></param>"
        /// <returns></returns>
        private AdaptorUserGroup CreateAdaptorUserGroup(Project project, string name, string description, string prefix)
        {
            return new AdaptorUserGroup
            {
                Name = string.Concat(prefix, name),
                AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>(),
                Description = description,
                Project = project
            };
        }

        /// <summary>
        /// Create auth credentaials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="keyPath"></param>
        /// <param name="passphrase"></param>
        /// <param name="publicKeyFingerprint"></param>
        /// <returns></returns>
        private ClusterAuthenticationCredentials CreateClusterAuthenticationCredentials(string username, string password, string keyPath, string passphrase, string publicKeyFingerprint)
        {
            return new ClusterAuthenticationCredentials
            {
                Username = username,
                Password = password,
                PrivateKeyFile = keyPath,
                PrivateKeyPassword = passphrase,
                AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKey,
                CipherType = CipherGeneratorConfiguration.Type,
                PublicKeyFingerprint = publicKeyFingerprint,
                ClusterProjectCredentials = new List<ClusterProjectCredential>(),
                IsGenerated = true
            };
        }

        /// <summary>
        /// Creates a new cluster reference to project and map credentials with the specified parameters
        /// </summary>
        /// <param name="clusterProject"></param>
        /// <param name="clusterAuthenticationCredentials"></param>
        /// <param name="isServiceAccount"></param>
        /// <returns></returns>
        private ClusterProjectCredential CreateClusterProjectCredentials(ClusterProject clusterProject, ClusterAuthenticationCredentials clusterAuthenticationCredentials, bool isServiceAccount)
        {
            return new ClusterProjectCredential
            {
                ClusterProject = clusterProject,
                ClusterAuthenticationCredentials = clusterAuthenticationCredentials,
                CreatedAt = System.DateTime.Now,
                IsDeleted = false,
                IsServiceAccount = isServiceAccount
            };
        }
        #endregion
    }
}

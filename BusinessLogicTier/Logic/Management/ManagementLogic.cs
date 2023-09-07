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
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

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
                                                             .CreateScheduler(cluster)
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
                                                             .CreateScheduler(cluster)
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
            var existingProject = _unitOfWork.ProjectRepository.GetByAccountingString(accountingString);
            if (existingProject != null)
            {
                var errorMessage = $"Project with accounting string {accountingString} already exists!";
                _logger.Error(errorMessage);
                throw new InputValidationException(errorMessage);
            }

            var project = InitializeProject(accountingString, usageType, name, description, startDate, endDate);

            var adaptorUserGroup = CreateAdaptorUserGroup(project, name, description);

            try
            {
                _unitOfWork.AdaptorUserGroupRepository.Insert(adaptorUserGroup);
                _unitOfWork.Save();

                var adminUserRole = new AdaptorUserUserGroupRole
                {
                    AdaptorUserId = loggedUser.Id,
                    AdaptorUserGroupId = adaptorUserGroup.Id,
                    AdaptorUserRoleId = (long)UserRoleType.Administrator
                };
                var allRoles = UserRoleUtils.GetAllUserRoles(new List<AdaptorUserUserGroupRole> { adminUserRole }).ToList();

                loggedUser.AdaptorUserUserGroupRoles.AddRange(allRoles);
                _unitOfWork.AdaptorUserRepository.Update(loggedUser);

                _unitOfWork.Save();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while creating project and adaptorUserGroup: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            return project;
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
                throw new Exception(errorMessage);
            }
            var clusters = _unitOfWork.ClusterRepository.GetById(clusterId);
            if (clusters == null)
            {
                var errorMessage = $"Cluster with id {clusterId} does not exist!";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            //Create cluster to project mapping
            var clusterProject = new ClusterProject
            {
                ClusterId = clusterId,
                ProjectId = projectId,
                LocalBasepath = localBasepath,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
            };

            try
            {
                _unitOfWork.ClusterProjectRepository.Insert(clusterProject);
                _unitOfWork.Save();
            }
            catch (Exception e)
            {
                var errorMessage = $"Error while assigning project to clusters: {e.Message}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            return clusterProject;
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
        /// <param name="projects"></param>
        /// <returns></returns>
        public SecureShellKey CreateSecureShellKey(string username, long[] projects)
        {
            //Chech if all project defined by AccountingString exist in HEAppE
            List<long> nonExistingProjects = new List<long>();
            List<Project> existingProjects = new List<Project>();

            foreach (long projectId in projects)
            {
                var project = _unitOfWork.ProjectRepository.GetById(projectId);
                if (project is null)
                {
                    nonExistingProjects.Add(projectId);
                }
                else
                {
                    existingProjects.Add(project);
                }
            }

            if (nonExistingProjects.Any())
            {
                _logger.Error($"The specified project with accounting string {string.Join(", ", nonExistingProjects)} is not defined in HEAppE!");
                throw new InputValidationException($"The specified project with accounting string {string.Join(", ", nonExistingProjects)} is not defined in HEAppE!");
            }

            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);
            string keyPath = GetUniquePrivateKeyPath(projects);
            //save private key to file
            FileInfo file = new FileInfo(keyPath);
            file.Directory.Create();
            File.WriteAllText(keyPath, secureShellKey.PrivateKeyPEM);

            foreach (var project in existingProjects)
            {
                _logger.Info($"Creating SSH key for user {username} for project {project.Name}.");
                var clusterProjects = _unitOfWork.ClusterProjectRepository.GetAll().Where(x => x.ProjectId == project.Id).ToList();

                ClusterAuthenticationCredentials serviceCredentials = CreateClusterAuthenticationCredentials(username, keyPath, passphrase, secureShellKey.PublicKeyFingerprint);
                ClusterAuthenticationCredentials nonServiceCredentials = CreateClusterAuthenticationCredentials(username, keyPath, passphrase, secureShellKey.PublicKeyFingerprint);

                foreach (var clusterProject in clusterProjects)
                {
                    serviceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, serviceCredentials, true));
                    nonServiceCredentials.ClusterProjectCredentials.Add(CreateClusterProjectCredentials(clusterProject, nonServiceCredentials, false));
                }

                _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(serviceCredentials);
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Insert(nonServiceCredentials);

                _unitOfWork.Save();
            }

            return secureShellKey;
        }

        /// <summary>
        /// Recreates encrypted SSH key for the specified user and saves it to the database.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        public SecureShellKey RecreateSecureShellKey(string username, string publicKey)
        {
            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint)
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
        public string RemoveSecureShellKey(string publicKey)
        {
            string publicKeyFingerprint = ComputePublicKeyFingerprint(publicKey);
            var clusterAuthenticationCredentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAllGeneratedWithFingerprint(publicKeyFingerprint)
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
                credentials.ClusterProjectCredentials.ForEach(cpc => cpc.IsDeleted = true);
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
        /// <returns></returns>
        private string GetUniquePrivateKeyPath(long[] projectIds)
        {
            string projectIdsString = string.Join("_", projectIds);
            long netxId = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Max(x => x.Id) + 1;
            string directoryPath = Path.Combine(_sshKeysDirectory, projectIdsString.ToUpper());
            string keyPath = Path.Combine(directoryPath, $"KEY_{projectIdsString}_{netxId}");
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
                Name = name ?? accountingString,
                Description = description,
                CreatedAt = DateTime.Now,
                StartDate = startDate,
                EndDate = endDate,
                UsageType = usageType,
                IsDeleted = false
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
        /// <returns></returns>
        private AdaptorUserGroup CreateAdaptorUserGroup(Project project, string name, string description)
        {
            return new AdaptorUserGroup
            {
                Name = name,
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
        private ClusterAuthenticationCredentials CreateClusterAuthenticationCredentials(string username, string keyPath, string passphrase, string publicKeyFingerprint)
        {
            return new ClusterAuthenticationCredentials
            {
                Username = username,
                Password = null,
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

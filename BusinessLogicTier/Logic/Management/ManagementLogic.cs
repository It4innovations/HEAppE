using HEAppE.BusinessLogicTier.Logic.Management.Exceptions;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.Management;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using log4net;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// <param name="accountingStrings"></param>
        /// <returns></returns>
        public SecureShellKey CreateSecureShellKey(string username, string[] accountingStrings)
        {
            //Chech if all project defined by AccountingString exist in HEAppE
            List<string> nonExistingProjects = new List<string>();
            List<Project> existingProjects = new List<Project>();

            foreach (string accountingString in accountingStrings)
            {
                var project = _unitOfWork.ProjectRepository.GetByAccountingString(accountingString);
                if (project is null)
                {
                    nonExistingProjects.Add(accountingString);
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
            string keyPath = GetUniquePrivateKeyPath(accountingStrings);
            //save private key to file
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

        private ClusterAuthenticationCredentials CreateClusterAuthenticationCredentials(string username, string keyPath, string passphrase, string publicKeyFingerprint)
        {
            return new ClusterAuthenticationCredentials
            {
                Username = username,
                Password = null,
                PrivateKeyFile = keyPath,
                PrivateKeyPassword = passphrase,
                AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKey,
                PublicKeyFingerprint = publicKeyFingerprint,
                ClusterProjectCredentials = new List<ClusterProjectCredentials>(),
                IsGenerated = true
            };
        }

        private ClusterProjectCredentials CreateClusterProjectCredentials(ClusterProject clusterProject, ClusterAuthenticationCredentials clusterAuthenticationCredentials, bool isServiceAccount)
        {
            return new ClusterProjectCredentials
            {
                ClusterProject = clusterProject,
                ClusterAuthenticationCredentials = clusterAuthenticationCredentials,
                CreatedAt = System.DateTime.Now,
                IsDeleted = false,
                IsServiceAccount = isServiceAccount
            };
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
                _unitOfWork.ClusterAuthenticationCredentialsRepository.Delete(credentials);
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
            Regex regex = new Regex(@"([A-Za-z0-9+\/=]+==)");
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

        private string GetUniquePrivateKeyPath(string[] projectIds)
        {
            string projectIdsString = string.Join("_", projectIds);
            long netxId = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetAll().Max(x => x.Id) + 1;
            string keyPath = Path.Combine(_sshKeysDirectory, $"KEY_{projectIdsString}_{netxId}");
            return keyPath;
        }
    }
}

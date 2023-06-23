using HEAppE.BusinessLogicTier.Logic.Management.Exceptions;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.Management;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public class ManagementLogic : IManagementLogic
    {
        protected IUnitOfWork _unitOfWork;
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
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (!commandTemplate.IsGeneric)
            {
                throw new InputValidationException("The specified command template is not generic.");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("The specified command template is deleted.");
            }

            var commandTemplateParameter = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                               .FirstOrDefault();

            if (string.IsNullOrEmpty(commandTemplateParameter?.Identifier))
            {
                throw new RequestedObjectDoesNotExistException("The user-script command parameter for the generic command template is not defined in HEAppE!");
            }

            if (string.IsNullOrEmpty(executableFile))
            {
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

            _unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
            _unitOfWork.Save();

            return newCommandTemplate;
        }

        public CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("The specified command template is deleted.");
            }

            if (commandTemplate.IsGeneric)
            {
                throw new InputValidationException("The specified command template is generic.");
            }

            if (executableFile is null)
            {
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
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

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
            SSHGenerator sshGenerator = new();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);
            Guid guid = Guid.NewGuid();
            string keyPath = Path.Combine(_sshKeysDirectory, guid.ToString());
            //save private key to file
            File.WriteAllText(keyPath, secureShellKey.PrivateKeyPEM);

            foreach (long projectId in projects)
            {
                var clusterProjects = _unitOfWork.ClusterProjectRepository.GetAll().Where(x => x.ProjectId == projectId).ToList();
                var serviceCredentials = new ClusterAuthenticationCredentials()
                {
                    Username = username,
                    Password = null,
                    PrivateKeyFile = keyPath,
                    PrivateKeyPassword = passphrase,
                    AuthenticationType = ClusterAuthenticationCredentialsAuthType.GeneratedKeyEncrypted,
                    PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint,
                    ClusterProjectCredentials = new List<ClusterProjectCredentials>()
                };

                var nonServiceCredentials = new ClusterAuthenticationCredentials()
                {
                    Username = username,
                    Password = null,
                    PrivateKeyFile = keyPath,
                    PrivateKeyPassword = passphrase,
                    AuthenticationType = ClusterAuthenticationCredentialsAuthType.GeneratedKeyEncrypted,
                    PublicKeyFingerprint = secureShellKey.PublicKeyFingerprint,
                    ClusterProjectCredentials = new List<ClusterProjectCredentials>()
                };

                foreach (var clusterProject in clusterProjects)
                {
                    serviceCredentials.ClusterProjectCredentials.Add(new ClusterProjectCredentials()
                    {
                        ClusterProject = clusterProject,
                        ClusterAuthenticationCredentials = serviceCredentials,
                        CreatedAt = System.DateTime.Now,
                        IsDeleted = false,
                        IsServiceAccount = true
                    });
                    nonServiceCredentials.ClusterProjectCredentials.Add(new ClusterProjectCredentials()
                    {
                        ClusterProject = clusterProject,
                        ClusterAuthenticationCredentials = nonServiceCredentials,
                        CreatedAt = System.DateTime.Now,
                        IsDeleted = false,
                        IsServiceAccount = false
                    });
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

            SSHGenerator sshGenerator = new SSHGenerator();
            string passphrase = StringUtils.GetRandomString();
            SecureShellKey secureShellKey = sshGenerator.GetEncryptedSecureShellKey(username, passphrase);
            Guid guid = Guid.NewGuid();

            foreach (var credentials in clusterAuthenticationCredentials)
            {
                File.Delete(credentials.PrivateKeyFile);

                string keyPath = Path.Combine(_sshKeysDirectory, guid.ToString());
                File.WriteAllText(keyPath, secureShellKey.PrivateKeyPEM);

                credentials.PrivateKeyFile = keyPath;
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
            Regex regex = new Regex(@"([A-Za-z0-9+\/=]+==)");
            Match match = regex.Match(publicKey);
            if (!match.Success)
            {
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
    }
}

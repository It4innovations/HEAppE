using Exceptions.External;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.DTO;
using HEAppE.ExternalAuthentication.KeyCloak;
using HEAppE.ExternalAuthentication.KeyCloak.Exceptions;
using HEAppE.OpenStackAPI;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.OpenStackAPI.Exceptions;
using HEAppE.Utils;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement
{
    internal class UserAndLimitationManagementLogic : IUserAndLimitationManagementLogic
    {
        #region Instances
        /// <summary>
        /// Unit of work
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Lock for creating user
        /// </summary>
        private readonly object _lockCreateUserObj = new();

        /// <summary>
        /// Session code expiration in seconds
        /// </summary>
        private static readonly int _sessionExpirationSeconds = BusinessLogicConfiguration.SessionExpirationInSeconds;
        #endregion
        #region Constructors
        internal UserAndLimitationManagementLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region Methods
        public AdaptorUser GetUserForSessionCode(string sessionCode)
        {
            SessionCode session = _unitOfWork.SessionCodeRepository.GetByUniqueCode(sessionCode);
            if (session is null)
            {
                _log.Error($"Session code \"{sessionCode}\" is not present in the database.");
                throw new SessionCodeNotValidException($"Session code \"{sessionCode}\" is not present in the database.");
            }

            if (IsSessionExpired(session))
            {
                _log.Warn($"Session code \"{sessionCode}\" already expired at \"{session.LastAccessTime.AddSeconds(_sessionExpirationSeconds)}\".");
                throw new SessionCodeNotValidException($"Session code \"{sessionCode}\" already expired at \"{session.LastAccessTime.AddSeconds(_sessionExpirationSeconds)}\".");
            }

            session.LastAccessTime = DateTime.UtcNow;
            _unitOfWork.SessionCodeRepository.Update(session);
            _unitOfWork.Save();
            return session.User;
        }

        public async Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials)
        {
            return credentials switch
            {
                PasswordCredentials => AuthenticateUserWithPassword(credentials as PasswordCredentials),
                DigitalSignatureCredentials => AuthenticateUserWithDigitalSignature(credentials as DigitalSignatureCredentials),
                OpenIdCredentials => CreateSessionCode(await HandleOpenIdAuthenticationAsync(credentials as OpenIdCredentials)).UniqueCode,
                _ => throw new ArgumentException($"Credentials of class {credentials.GetType().Name} are not supported. Change the UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.")
            };
        }

        public async Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials)
        {
            _log.Info($"User \"{credentials.Username}\" wants to authenticate to the OpenStack.");

            var user = await HandleOpenIdAuthenticationAsync(credentials);
            return user;
        }

        /// <summary>
        /// Create new session/application credentials for authenticated user.
        /// </summary>
        /// <param name="adaptorUser">User with access to OpenStack part of the HEAppE.</param>
        /// <param name="projectId">Project Id</param>
        /// <returns>OpenStack application credentials.</returns>
        /// <exception cref="AuthenticationException">is throws, if OpenStack service is inaccessible.</exception>
        public async Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser, long projectId)
        {
            try
            {
                _log.Info($"OpenId: user \"{adaptorUser.Username}\" wants to authenticate to the OpenStack project \"{projectId}\".");

                if (!adaptorUser.Groups.Any(f => f.ProjectId == projectId))
                {
                    throw new OpenStackAPIException($"User does not have permission to create OpenStack credentials for project \"{projectId}\"!");
                }

                var openStackProject = GetOpenStackInstanceWithProjects(projectId);
                bool hasRequiredRole = adaptorUser.AdaptorUserUserGroupRoles.Any(x => (UserRoleType)x.AdaptorUserRoleId == UserRoleType.Submitter
                                                                                                    && x.AdaptorUserGroup.ProjectId == openStackProject.HEAppEProjectId
                                                                                                    && !x.AdaptorUserGroup.Project.IsDeleted
                                                                                                    && x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow);

                if (!hasRequiredRole)
                {
                    using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                    var requiredRoleModel = unitOfWork.AdaptorUserRoleRepository.GetById((long)UserRoleType.Submitter);
                    throw InsufficientRoleException.CreateMissingRoleException(requiredRoleModel, adaptorUser.GetRolesForProject(openStackProject.HEAppEProjectId.Value), openStackProject.HEAppEProjectId.Value);
                }

                var openStack = new OpenStack(openStackProject.Domain.InstanceUrl);
                var authResponse = await openStack.AuthenticateAsync(openStackProject);
                var openStackCredentials = await openStack.CreateApplicationCredentialsAsync(adaptorUser.Username, authResponse);

                var openStackSession = new OpenStackSession
                {
                    UserId = adaptorUser.Id,
                    AuthenticationTime = DateTime.UtcNow,
                    ExpirationTime = openStackCredentials.ExpiresAt,
                    ApplicationCredentialsId = openStackCredentials.ApplicationCredentialsId,
                    ApplicationCredentialsSecret = openStackCredentials.ApplicationCredentialsSecret
                };

                _unitOfWork.OpenStackSessionRepository.Insert(openStackSession);
                _unitOfWork.Save();

                _log.Info($"Created new OpenStack 'session' (application credentials) for user \"{adaptorUser.Username}\".");
                return openStackCredentials;
            }
            catch (OpenStackAPIException ex)
            {
                _log.Error($"Failed to retrieve OpenStack token for authorized OpenId user: \"{adaptorUser.Username}\". Reason: \"{ex.Message}\"");
                throw new AuthenticationException("Unable to retrieve OpenStack application credentials for this user.");
            }
        }

        public AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser)
        {
            return loggedUser.Groups.FirstOrDefault() ?? _unitOfWork.AdaptorUserGroupRepository.GetDefaultSubmitterGroup();
        }

        public bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo)
        {
            return jobInfo.Submitter.Id == loggedUser.Id;
        }

        public bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo)
        {
            return taskInfo.Specification.JobSpecification.Submitter.Id == loggedUser.Id;
        }

        public IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser)
        {
            var notFinishedJobs = LogicFactory.GetLogicFactory()
                                                                      .CreateJobManagementLogic(_unitOfWork)
                                                                      .GetNotFinishedJobInfosForSubmitterId(loggedUser.Id);
            var nodeTypes = LogicFactory.GetLogicFactory()
                                                                .CreateClusterInformationLogic(_unitOfWork)
                                                                .ListClusterNodeTypes();

            IList<ResourceUsage> result = new List<ResourceUsage>(nodeTypes.Count());
            foreach (ClusterNodeType nodeType in nodeTypes)
            {
                var usage = new ResourceUsage
                {
                    NodeType = nodeType,
                    CoresUsed = notFinishedJobs.Sum(s => s.Tasks.Sum(taskSum => taskSum.Specification.MaxCores)) ?? 0,
                    Limitation = loggedUser.Limitations.Where(w => w.NodeType == nodeType).FirstOrDefault()
                };
                result.Add(usage);
            }

            return result;
        }

        public IList<ProjectResourceUsage> CurrentUsageAndLimitationsForUserByProject(AdaptorUser loggedUser)
        {
            var allUserJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterId(loggedUser.Id);
            var projects = _unitOfWork.ProjectRepository.GetAll();

            IList<ProjectResourceUsage> result = new List<ProjectResourceUsage>();
            foreach (var project in projects)
            {
                var usage = new ProjectResourceUsage
                {
                    Id = project.Id,
                    AccountingString = project.AccountingString,
                    CreatedAt = project.CreatedAt,
                    Description = project.Description,
                    EndDate = project.EndDate,
                    ModifiedAt = project.ModifiedAt,
                    StartDate = project.StartDate,
                    IsDeleted = project.IsDeleted,
                    Name = project.Name,
                    NodeTypes = new List<ClusterNodeTypeResourceUsage>()
                };

                var projectCommandTemplates = _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(project.Id);
                var nodeTypes = projectCommandTemplates.Select(x => x.ClusterNodeType).ToList().Distinct();
                foreach (ClusterNodeType nodeType in nodeTypes)
                {
                    var tasksAtNode = allUserJobs.SelectMany(x => x.Tasks).Where(x => x.NodeType == nodeType);
                    var clusterNodeUsedCoresAndLimitation = new NodeUsedCoresAndLimitation()
                    {
                        CoresUsed = tasksAtNode.Sum(taskSum => taskSum.AllocatedCores) ?? 0,
                        Limitation = loggedUser.Limitations.Where(w => w.NodeType == nodeType).FirstOrDefault(),
                        NodeType = nodeType
                    };
                    var clusterNodeTypeUsage = new ClusterNodeTypeResourceUsage()
                    {
                        Id = nodeType.Id,
                        Name = nodeType.Name,
                        Cluster = nodeType.Cluster,
                        ClusterAllocationName = nodeType.ClusterAllocationName,
                        CoresPerNode = nodeType.CoresPerNode,
                        Description = nodeType.Description,
                        FileTransferMethod = nodeType.FileTransferMethod,
                        MaxWalltime = nodeType.MaxWalltime,
                        NumberOfNodes = nodeType.NumberOfNodes,
                        Queue = nodeType.Queue,
                        NodeUsedCoresAndLimitation = clusterNodeUsedCoresAndLimitation
                    };
                    usage.NodeTypes.Add(clusterNodeTypeUsage);
                }
                result.Add(usage);
            }
            return result;
        }
        #endregion
        #region Private Methods
        private OpenStackProjectDTO GetOpenStackInstanceWithProjects(long? projectId)
        {
            var project = _unitOfWork.OpenStackProjectRepository.GetOpenStackProjectByProjectId(projectId.Value);
            if (project is null)
            {
                throw new OpenStackAPIException($"There are not defined OpenStack project \"{projectId}\"!");
            }

            var projectCredentials = project.OpenStackAuthenticationCredentialProjects.FirstOrDefault(f => f.IsDefault) ?? project.OpenStackAuthenticationCredentialProjects.FirstOrDefault();

            if (projectCredentials is null)
            {
                throw new OpenStackAPIException($"There are not assigned user credentials for OpenStack project \"{projectId}\"!");
            }

            return new OpenStackProjectDTO()
            {
                Name = project.Name,
                UID = project.UID,
                HEAppEProjectId = project.AdaptorUserGroup.ProjectId,
                ProjectDomain = new OpenStackProjectDomainDTO()
                {
                    UID = project.OpenStackProjectDomain.UID,
                    Name = project.OpenStackProjectDomain.Name
                },
                Domain = new OpenStackDomainDTO()
                {
                    UID = project.OpenStackDomain.UID,
                    Name = project.OpenStackDomain.Name,
                    InstanceUrl = project.OpenStackDomain.OpenStackInstance.InstanceUrl
                },
                Credentials = new OpenStackCredentialsDTO()
                {
                    Id = projectCredentials.OpenStackAuthenticationCredential.UserId,
                    Username = projectCredentials.OpenStackAuthenticationCredential.Username,
                    Password = projectCredentials.OpenStackAuthenticationCredential.Password
                }
            };
        }

        /// <summary>
        /// Check the provided OpenId tokens, refresh the token and the pass to logic to user retrieval.
        /// </summary>
        /// <param name="openIdCredentials">OpenId credentials.</param>
        /// <returns>New or existing user.</returns>
        private async Task<AdaptorUser> HandleOpenIdAuthenticationAsync(OpenIdCredentials openIdCredentials)
        {
            try
            {
                var openIdClient = new KeycloakOpenId();
                var tokenIntrospectResult = await openIdClient.TokenIntrospectionAsync(openIdCredentials.OpenIdAccessToken);
                KeycloakOpenId.ValidateUserToken(tokenIntrospectResult);

                string offline_token = (await openIdClient.ExchangeTokenAsync(openIdCredentials.OpenIdAccessToken)).AccessToken;
                var userInfo = await openIdClient.GetUserInfoAsync(offline_token);

                return GetOrRegisterNewOpenIdUser(userInfo.Convert());
            }
            catch (KeycloakOpenIdException keycloakException)
            {
                _log.Error($"OpenId: Failed to authenticate user via access token. access_token='{openIdCredentials.OpenIdAccessToken}'", keycloakException);
                throw new OpenIdAuthenticationException("Invalid or not active OpenId token provided. Unable to authenticate user by provided credentials.", keycloakException);
            }
            catch (OpenIdAuthenticationException OpenIdException)
            {
                _log.Error($"OpenId: Failed to authenticate user via access token. access_token='{openIdCredentials.OpenIdAccessToken}'", OpenIdException);
                throw new OpenIdAuthenticationException("Invalid or not active OpenId token provided. Unable to authenticate user by provided credentials.", OpenIdException);
            }
        }

        /// <summary>
        /// Get existing or create new HEAppE user, from the OpenId credentials.
        /// </summary>
        /// <param name="decodedAccessToken">Decoded OpenId access token.</param>
        /// <returns>Newly created or existing HEAppE account.</returns>
        private AdaptorUser GetOrRegisterNewOpenIdUser(UserOpenId openIdUser)
        {
            lock (_lockCreateUserObj)
            {
                var changedTime = DateTime.UtcNow;
                AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
                if (user is null)
                {
                    user = new AdaptorUser
                    {
                        Username = openIdUser.UserName,
                        Deleted = false,
                        Synchronize = false,
                        LanguageId = 1,
                        Email = openIdUser.Email,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = null
                    };

                    _unitOfWork.AdaptorUserRepository.Insert(user);
                    _unitOfWork.Save();
                    _log.Info($"OpenId: Created new HEAppE account for user: \"{user}\"");
                }

                var userGroupRoles = new List<AdaptorUserUserGroupRole>();
                bool hasUserGroup = false;
                foreach (var project in openIdUser.Projects)
                {
                    if (!TryGetUserGroupByName(project.HEAppEGroupName, out AdaptorUserGroup openIdGroup))
                    {
                        _log.Warn($"OpenId: User group(\"{project.HEAppEGroupName}\") does not exist in HEAppE database!");
                        continue;
                    }

                    hasUserGroup = true;

                    var databaseGroupRoles = user.AdaptorUserUserGroupRoles.Where(w => w.AdaptorUserGroup == openIdGroup).ToList();
                    var groupRoles = _unitOfWork.AdaptorUserRoleRepository.GetAllByRoleNames(project.Roles);

                    //Set roles for group to IsDeleted
                    databaseGroupRoles.ForEach(f => { f.IsDeleted = true; f.ModifiedAt = changedTime; });

                    foreach (var groupRole in groupRoles)
                    {
                        var dbGroupRole = databaseGroupRoles.FirstOrDefault(f => f.AdaptorUserRole.Name == groupRole.Name);

                        if (dbGroupRole is null)
                        {
                            userGroupRoles.Add(new AdaptorUserUserGroupRole()
                            {
                                AdaptorUser = user,
                                AdaptorUserGroup = openIdGroup,
                                AdaptorUserRole = groupRole,
                                IsDeleted = false,
                                CreatedAt = changedTime
                            });
                        }
                        else
                        {
                            dbGroupRole.IsDeleted = false;
                            dbGroupRole.ModifiedAt = changedTime;
                        }
                    }
                    _log.Info($"OpenId: User \"{user.Username}\" was added to group: \"{openIdGroup.Name}\"");
                }

                if (!hasUserGroup)
                {
                    user.AdaptorUserUserGroupRoles.ForEach(f => { f.IsDeleted = true; f.ModifiedAt = changedTime; });
                    _unitOfWork.Save();
                    throw new OpenIdAuthenticationException($"Open-d: User(\"{user.Username}\") has not User group!");
                }
                else
                {
                    user.AdaptorUserUserGroupRoles.AddRange(userGroupRoles);
                    _unitOfWork.Save();
                }

                return user;
            }
        }

        /// <summary>
        /// Tries to get user group from database by its unique name.
        /// </summary>
        /// <param name="groupName">User group name.</param>
        /// <param name="userGroup">Retrieved user group.</param>
        /// <returns>True if group is found.</returns>
        private bool TryGetUserGroupByName(string groupName, out AdaptorUserGroup userGroup)
        {
            userGroup = _unitOfWork.AdaptorUserGroupRepository.GetGroupByUniqueName(groupName);
            return userGroup is not null;
        }

        /// <summary>
        /// Used for generation https://www.convertstring.com/en/Hash/SHA512
        /// </summary>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        /// <exception cref="InvalidAuthenticationCredentialsException"></exception>
        private string AuthenticateUserWithPassword(PasswordCredentials credentials)
        {
            AdaptorUser user = GetActiveUser(credentials.Username);

            byte[] inputBytes = Encoding.UTF8.GetBytes(credentials.Password);
            byte[] saltBytes = Encoding.UTF8.GetBytes(user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            byte[] cipherBytes = inputBytes.Concat(saltBytes).ToArray();

            byte[] hashBytes = SHA512.Create().ComputeHash(cipherBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            string hash = sb.ToString();

            if (hash != user.Password)
            {
                string message = $"Authentication of user \"{user.Username}\" was not successful due to wrong credentials.";
                _log.Error(message);
                throw new InvalidAuthenticationCredentialsException(message);
            }

            return CreateSessionCode(user).UniqueCode;
        }

        private string AuthenticateUserWithDigitalSignature(DigitalSignatureCredentials credentials)
        {
            AdaptorUser user = GetActiveUser(credentials.Username);
            byte[] hash; // Hash of signed data

            // Hash data
            using (SHA256 hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(credentials.SignedContent));
            }

            // Verify digital signature
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                // TODO: Verify
                rsa.FromXmlString(user.PublicKey);
                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                if (rsaDeformatter.VerifySignature(hash, credentials.DigitalSignature))
                {
                    return CreateSessionCode(user).UniqueCode;
                }

                _log.Error($"Authentication of user \"{user.Username}\" was not successful due to wrong credentials.");
                throw new InvalidAuthenticationCredentialsException($"Authentication of user \"{user.Username}\" was not successful due to wrong credentials.");
            }
        }

        private AdaptorUser GetActiveUser(string username)
        {
            AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetByName(username);
            _log.Info($"User \"{username}\" wants to authenticate to the system.");

            if (user == null)
            {
                string wrongCredentialsError = $"Authentication of user \"{username}\" was not successful due to wrong credentials.";
                _log.Error(wrongCredentialsError);
                throw new InvalidAuthenticationCredentialsException(wrongCredentialsError);
            }

            if (user.Deleted)
            {
                string deletedUserError = $"User \"{user.Username}\", that requested authentication was already deleted from the system.";
                _log.Error(deletedUserError);
                throw new AuthenticatedUserAlreadyDeletedException(deletedUserError);
            }
            return user;
        }

        private SessionCode CreateSessionCode(AdaptorUser user)
        {
            SessionCode sessionCode = _unitOfWork.SessionCodeRepository.GetByUser(user);

            if (sessionCode is null || IsSessionExpired(sessionCode))
            {
                Guid guid;
                do
                {
                    guid = Guid.NewGuid();
                } while (_unitOfWork.SessionCodeRepository.GetByUniqueCode(guid.ToString()) != null);

                sessionCode = new SessionCode
                {
                    AuthenticationTime = DateTime.UtcNow,
                    LastAccessTime = DateTime.UtcNow,
                    UniqueCode = guid.ToString(),
                    User = user
                };
                _unitOfWork.SessionCodeRepository.Insert(sessionCode);
            }
            else
            {
                sessionCode.LastAccessTime = DateTime.UtcNow;
                _unitOfWork.SessionCodeRepository.Update(sessionCode);
            }

            _unitOfWork.Save();
            return sessionCode;
        }

        private static bool IsSessionExpired(SessionCode session)
        {
            return session.LastAccessTime < DateTime.UtcNow.AddSeconds(-_sessionExpirationSeconds);
        }

        public IEnumerable<ProjectReference> ProjectsForCurrentUser(AdaptorUser loggedUser)
        {
            var projectReferences = new List<ProjectReference>();
            var groupRoles = loggedUser.AdaptorUserUserGroupRoles.GroupBy(x => x.AdaptorUserGroup).Select(g => g.OrderBy(x => x.AdaptorUserRoleId).First());
            foreach (var groupRole in groupRoles)
            {
                var project = _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndProject().FirstOrDefault(x => x.Id == groupRole.AdaptorUserGroupId)?.Project;
                var commandTemplates = _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(project.Id);

                project.CommandTemplates = commandTemplates.ToList();

                projectReferences.Add(new()
                {
                    Role = groupRole.AdaptorUserRole,
                    Project = project
                });
            }
            return projectReferences.Distinct();
        }
        #endregion
    }
}
using HEAppE.KeycloakOpenIdAuthentication;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using HEAppE.DomainObjects.OpenStack;
using HEAppE.KeycloakOpenIdAuthentication.Exceptions;
using HEAppE.OpenStackAPI;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.OpenStackAPI.Exceptions;
using HEAppE.KeycloakOpenIdAuthentication.Configuration;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Configuration;

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
        /// OpenStack instance
        /// </summary>
        private static OpenStackInfoDTO _openStackInstance = null;

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

        public AdaptorUser GetUserForSessionCode(string sessionCode)
        {
            SessionCode session = _unitOfWork.SessionCodeRepository.GetByUniqueCode(sessionCode);
            if (session == null)
            {
                _log.Error("Session code " + sessionCode + " is not present in the database.");
                throw new SessionCodeNotValidException("Session code " + sessionCode +
                                                       " is not valid. Please go through the authentication process.");
            }

            if (IsSessionExpired(session))
            {
                _log.Warn("Session code " + sessionCode + " already expired at " +
                         session.LastAccessTime.AddSeconds(_sessionExpirationSeconds) + ".");
                throw new SessionCodeNotValidException("Session code " + sessionCode +
                                                       " already expired. Please repeat the authentication process.");
            }

            session.LastAccessTime = DateTime.UtcNow;
            _unitOfWork.SessionCodeRepository.Update(session);
            _unitOfWork.Save();
            return session.User;
        }

        public async Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials)
        {
            AdaptorUser user;
            if (credentials is OpenIdCredentials)
            {
                user = await HandleOpenIdAuthenticationAsync(credentials as OpenIdCredentials);
                _log.Info($"KeyCloak user {user.Username} wants to authenticate to the system.");
            }
            else
            {
                // Get standard HEAppE user
                user = _unitOfWork.AdaptorUserRepository.GetByName(credentials.Username);
                _log.Info($"User {credentials.Username} wants to authenticate to the system.");
            }


            if (user == null)
            {
                string wrongCredentialsError =  $"Authentication of user {credentials.Username} was not successful due to wrong credentials.";
                _log.Error(wrongCredentialsError);
                throw new InvalidAuthenticationCredentialsException(wrongCredentialsError);
            }

            if (user.Deleted)
            {
                string deletedUserError = $"User {user.Username} that requested authentication was already deleted from the system.";
                _log.Error(deletedUserError);
                throw new AuthenticatedUserAlreadyDeletedException(deletedUserError);
            }

            switch (credentials)
            {
                case DigitalSignatureCredentials:
                    return AuthenticateUserWithDigitalSignature(user, (DigitalSignatureCredentials)credentials);
                case PasswordCredentials:
                    return AuthenticateUserWithPassword(user, (PasswordCredentials)credentials);
                case OpenIdCredentials:
                    // NOTE: We can just create session code for the user because GetOrRegisterNewOpenIdUser didn't fail and the OpenId token is valid.
                    return CreateSessionCode(user).UniqueCode;
            }

            string unsupportedCredentialsError = $"Credentials of class {credentials.GetType().Name} are not supported. Change the UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser()" +
                                                 $" method to add support for additional credential types.";
            _log.Error(unsupportedCredentialsError);
            throw new ArgumentException(unsupportedCredentialsError);
        }

        public async Task<AdaptorUser> AuthenticateUserToOpenStackAsync(AuthenticationCredentials credentials)
        {
            _log.Info($"User {credentials.Username} wants to authenticate to the OpenStack.");
            if (credentials is not OpenIdCredentials openIdCredentials)
            {
                string unsupportedCredentialsError = $"Credentials of class {credentials.GetType().Name} are not supported.";
                _log.Error(unsupportedCredentialsError);
                throw new ArgumentException(unsupportedCredentialsError);
            }
            var user = await HandleOpenIdAuthenticationAsync(openIdCredentials);
            return user;
        }

        public async Task<ApplicationCredentialsDTO> AuthenticateKeycloakUserToOpenStackAsync(AdaptorUser adaptorUser)
        {
            _log.Info($"Keycloak user {adaptorUser.Username} wants to authenticate to the OpenStack.");
            return await CreateNewOpenStackSessionAsync(adaptorUser);
        }

        private OpenStackInfoDTO GetOpenStackInstanceWithProjects()
        {
            OpenStackAuthenticationCredential osInstanceCredentials = _unitOfWork.OpenStackAuthenticationCredentialsRepository.GetDefaultAccount();
            var projectDictionary = new Dictionary<OpenStackProjectDTO, List<OpenStackProjectDomainDTO>>();

            foreach (var DbProjectDomain in osInstanceCredentials.OpenStackAuthenticationCredentialProjectDomains)
            {
                var projectDomain = new OpenStackProjectDomainDTO()
                {
                    Id = DbProjectDomain.OpenStackProjectDomain.UID,
                    Name = DbProjectDomain.OpenStackProjectDomain.Name
                };

                var projectDomains = new List<OpenStackProjectDomainDTO>() { projectDomain };

                var project = new OpenStackProjectDTO()
                {
                    Id = DbProjectDomain.OpenStackProjectDomain.OpenStackProject.UID,
                    Name = DbProjectDomain.OpenStackProjectDomain.OpenStackProject.Name,
                    Domain = new OpenStackDomainDTO()
                    {
                        Name = DbProjectDomain.OpenStackProjectDomain.OpenStackProject.OpenStackDomain.Name,
                        InstanceUrl = DbProjectDomain.OpenStackProjectDomain.OpenStackProject.OpenStackDomain.OpenStackInstance.InstanceUrl
                    },
                    ProjectDomains = projectDomains
                };

                if (projectDictionary.ContainsKey(project))
                {
                    projectDictionary[project].Add(projectDomain);
                }
                else
                {
                    projectDictionary.Add(project, projectDomains);
                }
            }

            return new OpenStackInfoDTO()
            {
                Projects = projectDictionary.Keys,
                ServiceAcc = new OpenStackServiceAccDTO()
                {
                    Id = osInstanceCredentials.UserId,
                    Username = osInstanceCredentials.Username,
                    Password = osInstanceCredentials.Password,
                }
            };
        }

        /// <summary>
        /// Create new session/application credentials for authenticated user.
        /// </summary>
        /// <param name="userAccount">User with access to OpenStack part of the HEAppE.</param>
        /// <returns>OpenStack application credentials.</returns>
        /// <exception cref="AuthenticationException">is throws, if OpenStack service is inaccessible.</exception>
        private async Task<ApplicationCredentialsDTO> CreateNewOpenStackSessionAsync(AdaptorUser userAccount)
        {
            if (_openStackInstance is null)
            {
                _openStackInstance = GetOpenStackInstanceWithProjects();
            }

            ApplicationCredentialsDTO openStackCredentials;
            try
            {
                var userGroupsName = userAccount.AdaptorUserUserGroups.Select(s => s.AdaptorUserGroup.AccountingString.ToLower())
                                                                        .ToList();

                var openStackProject = _openStackInstance.Projects.Where(w => userGroupsName.Intersect(w.ProjectDomains.Select(s => s.Name.ToLower()))
                                                                                                                        .Any() 
                                                                              || userGroupsName.Contains(w.Name.ToLower()))
                                                                    .FirstOrDefault();
                if (openStackProject is null)
                {
                    throw new OpenStackAPIException("OpenStack project domains have not corresponding accounting string in AdaptorUserGroup.");
                }
                else
                {
                    var openStack = new OpenStack(openStackProject.Domain.InstanceUrl);
                    var authResponse = await openStack.AuthenticateAsync(_openStackInstance.ServiceAcc, openStackProject);
                    openStackCredentials = await openStack.CreateApplicationCredentialsAsync(userAccount.Username, authResponse);
                }
            }
            catch (OpenStackAPIException ex)
            {
                _log.Error($"Failed to retrieve OpenStack token for authorized Keycloak user: {userAccount.Username}. Reason: {ex.Message}");
                throw new AuthenticationException("Unable to retrieve OpenStack application credentials for this user.");
            }

            StoreOpenStackSession(userAccount, openStackCredentials);
            _log.Info($"Created new OpenStack 'session' (application credentials) for user {userAccount.Username}.");
            return openStackCredentials;
        }

        /// <summary>
        /// Check the provided OpenId tokens, refresh the token and the pass to logic to user retrieval.
        /// </summary>
        /// <param name="openIdCredentials">OpenId tokens.</param>
        /// <returns>New or existing user.</returns>
        private async Task<AdaptorUser> HandleOpenIdAuthenticationAsync(OpenIdCredentials openIdCredentials)
        {
            try
            {
                var keycloak = new KeycloakOpenId();
                var tokenIntrospectResult = await keycloak.TokenIntrospectionAsync(openIdCredentials.OpenIdAccessToken);
                KeycloakOpenId.ValidateUserToken(tokenIntrospectResult);

                string offline_token = (await keycloak.ExchangeTokenAsync(openIdCredentials.OpenIdAccessToken)).AccessToken;
                var userInfo = await keycloak.GetUserInfoAsync(offline_token);

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

        private void SynchonizeKeycloakUserGroupAndRoles(AdaptorUser user, UserOpenId openIdUser)
        {
            if (!TryGetUserGroupByName(KeycloakConfiguration.HEAppEGroupName, out var keycloakGroup))
            {
                throw new OpenIdAuthenticationException("Keycloak group doesn't exist. Keycloak user's group and roles can't be synchronized.");
            }

            // Check if the user is in the keycloak group.
            if (!user.AdaptorUserUserGroups.Any(userGroup => userGroup.AdaptorUserGroupId == keycloakGroup.Id))
            {
                // Assign user to the group.
                if (!AddUserToGroup(user, keycloakGroup))
                {
                    throw new OpenIdAuthenticationException("Failed to assign user to the group. Check the log for details.");
                }

                _log.Info($"Open-Id: User '{user.Username}' was added to group: '{keycloakGroup.Name}'");
            }

            if (!openIdUser.ProjectRoles.TryGetValue(KeycloakConfiguration.Project, out IEnumerable<string> openIdRoles))
            {
                throw new OpenIdAuthenticationException("No roles for specific project is set");
            }

            var availableRoles = _unitOfWork.AdaptorUserRoleRepository.GetAllByRoleNames(openIdRoles).ToList();
            var userRoles = _unitOfWork.AdaptorUserRoleRepository.GetAllByUserId(user.Id).ToList();

            if (!(availableRoles.Count == userRoles.Count && availableRoles.Count == availableRoles.Intersect(userRoles).Count()))
            {
                user.AdaptorUserUserRoles = availableRoles.Select(s => new AdaptorUserUserRole
                {
                    AdaptorUserId = user.Id,
                    AdaptorUserRoleId = s.Id
                }).ToList();

                try
                {
                    _unitOfWork.Save();
                    _log.Info($"Open-Id: User '{user.Username}' has new roles: '{string.Join(",", availableRoles.Select(role => role.Name)) ?? "None"}' old roles were: '{string.Join(",", userRoles.Select(role => role.Name)) ?? "None"}'");
                }
                catch (Exception e)
                {
                    _log.Error("Failed to set user roles.", e);
                }
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
                AdaptorUser userAccount = _unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
                if (userAccount is null)
                {
                    // Create new Keycloak user account.
                    userAccount = new AdaptorUser
                    {
                        Username = openIdUser.UserName,
                        Deleted = false,
                        Synchronize = false,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = null
                    };

                    _unitOfWork.AdaptorUserRepository.Insert(userAccount);
                    _unitOfWork.Save();
                    _log.Info($"Open-Id: Created new HEAppE account for Keycloak OpenId user: '{userAccount}'");
                }

                SynchonizeKeycloakUserGroupAndRoles(userAccount, openIdUser);
                return userAccount;
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
        /// Add user account to the user group.
        /// </summary>
        /// <param name="userAccount">User account.</param>
        /// <param name="group">User group.</param>
        /// <returns>True if user was added to the group.</returns>
        private bool AddUserToGroup(AdaptorUser userAccount, AdaptorUserGroup group)
        {
            userAccount.AdaptorUserUserGroups.Add(new AdaptorUserUserGroup
            {
                AdaptorUserId = userAccount.Id,
                AdaptorUserGroupId = group.Id
            });

            try
            {
                _unitOfWork.Save();
                return true;
            }
            catch (Exception e)
            {
                _log.Error("Failed to add user to group.", e);
                return false;
            }
        }

        private string AuthenticateUserWithDigitalSignature(AdaptorUser user, DigitalSignatureCredentials credentials)
        {
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

                _log.Error("Authentication of user " + user.Username + " was not successful due to wrong credentials.");
                throw new InvalidAuthenticationCredentialsException("Authentication of user " + user.Username +
                                                                    " was not successful due to wrong credentials.");
            }
        }

        /// <summary>
        /// Used for generation https://www.convertstring.com/en/Hash/SHA512
        /// </summary>
        /// <param name="user"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        /// <exception cref="InvalidAuthenticationCredentialsException"></exception>
        private string AuthenticateUserWithPassword(AdaptorUser user, PasswordCredentials credentials)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(credentials.Password);
            byte[] saltBytes = Encoding.UTF8.GetBytes(user.CreatedAt.ToString());
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
                ResourceUsage usage = new ResourceUsage
                {
                    NodeType = nodeType,
                    CoresUsed = notFinishedJobs.Sum(s => s.Tasks.Sum(taskSum => taskSum.Specification.MaxCores)) ?? 0,
                    Limitation = loggedUser.Limitations.Where(w => w.NodeType == nodeType).FirstOrDefault()
                };
                result.Add(usage);
            }

            return result;
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

        private bool IsSessionExpired(SessionCode session)
        {
            return session.LastAccessTime.AddSeconds(_sessionExpirationSeconds) > DateTime.UtcNow;
        }

        /// <summary>
        /// Store OpenStack application credentials to database.
        /// </summary>
        /// <param name="user">User for which was the OpenStack session created.</param>
        /// <param name="applicationCredentials">Created OpenStack credentials.</param>
        /// <param name="expiresAt">Expiration of OpenStack credentials.</param>
        private void StoreOpenStackSession(AdaptorUser user, ApplicationCredentialsDTO applicationCredentials)
        {
            OpenStackSession session = new OpenStackSession
            {
                UserId = user.Id,
                AuthenticationTime = DateTime.UtcNow,
                ExpirationTime = applicationCredentials.ExpiresAt,
                ApplicationCredentialsId = applicationCredentials.ApplicationCredentialsId,
                ApplicationCredentialsSecret = applicationCredentials.ApplicationCredentialsSecret
            };
            _unitOfWork.OpenStackSessionRepository.Insert(session);
            _unitOfWork.Save();
        }

        private SessionCode CreateSessionCode(AdaptorUser user)
        {
            SessionCode code = _unitOfWork.SessionCodeRepository.GetByUser(user);

            if (!(code == null || IsSessionExpired(code)))
            {
                // User already has session, return it
                // Update auth time to current time and update db
                code.LastAccessTime = DateTime.UtcNow;
                _unitOfWork.SessionCodeRepository.Update(code);
                _unitOfWork.Save();
                return code;
            }

            // User does not have any session or session has expired
            // create new session
            return CreateNewSession(user);
        }

        private SessionCode CreateNewSession(AdaptorUser user)
        {
            // Generate new random code
            Guid guid;
            do
            {
                guid = Guid.NewGuid();
            } while (_unitOfWork.SessionCodeRepository.GetByUniqueCode(guid.ToString()) != null);

            // Put it into db
            SessionCode sessionCode = new SessionCode
            {
                AuthenticationTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                UniqueCode = guid.ToString(),
                User = user
            };
            _unitOfWork.SessionCodeRepository.Insert(sessionCode);
            _unitOfWork.Save();
            return sessionCode;
        }
    }
}
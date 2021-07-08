using HEAppE.KeycloakOpenIdAuthentication;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
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

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement
{
    internal class UserAndLimitationManagementLogic : IUserAndLimitationManagementLogic
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly object _lockCreateUserObj = new();
        private readonly IUnitOfWork unitOfWork;
        private const int cSaltBytes = 12;
        private const int cHashBytes = 20;

        //TO DO add to settings
        private static readonly int SessionExpirationSeconds = 900;

        internal UserAndLimitationManagementLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public AdaptorUser GetUserForSessionCode(string sessionCode)
        {
            SessionCode session = unitOfWork.SessionCodeRepository.GetByUniqueCode(sessionCode);
            if (session == null)
            {
                log.Error("Session code " + sessionCode + " is not present in the database.");
                throw new SessionCodeNotValidException("Session code " + sessionCode +
                                                       " is not valid. Please go through the authentication process.");
            }

            if (IsSessionExpired(session))
            {
                log.Warn("Session code " + sessionCode + " already expired at " +
                         session.LastAccessTime.AddSeconds(SessionExpirationSeconds) + ".");
                throw new SessionCodeNotValidException("Session code " + sessionCode +
                                                       " already expired. Please repeat the authentication process.");
            }

            session.LastAccessTime = DateTime.UtcNow;
            unitOfWork.SessionCodeRepository.Update(session);
            unitOfWork.Save();
            return session.User;
        }

        public string AuthenticateUser(AuthenticationCredentials credentials)
        {
            AdaptorUser user;
            if (credentials is OpenIdCredentials)
            {
                user = HandleOpenIdAuthentication(credentials as OpenIdCredentials);
                log.Info($"KeyCloak user {user.Username} wants to authenticate to the system.");
            }
            else
            {
                // Get standard HEAppE user
                user = unitOfWork.AdaptorUserRepository.GetByName(credentials.Username);
                log.Info($"User {credentials.Username} wants to authenticate to the system.");
            }


            if (user == null)
            {
                string wrongCredentialsError =
                    $"Authentication of user {credentials.Username} was not successful due to wrong credentials.";
                log.Error(wrongCredentialsError);
                throw new InvalidAuthenticationCredentialsException(wrongCredentialsError);
            }

            if (user.Deleted)
            {
                string deletedUserError = $"User {user.Username} that requested authentication was already deleted from the system.";
                log.Error(deletedUserError);
                throw new AuthenticatedUserAlreadyDeletedException(deletedUserError);
            }

            switch (credentials)
            {
                case DigitalSignatureCredentials:
                    return AuthenticateUserWithDigitalSignature(user, (DigitalSignatureCredentials)credentials);
                case PasswordCredentials:
                    return AuthenticateUserWithPassword(user, (PasswordCredentials)credentials);
                case OpenIdCredentials:
                    // NOTE(Moravec): We can just create session code for the user because GetOrRegisterNewOpenIdUser didn't fail and the OpenId token is valid.
                    return CreateSessionCode(user).UniqueCode;
            }

            string unsupportedCredentialsError = $"Credentials of class {credentials.GetType().Name} are not supported. Change the UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser()" +
                                                 $" method to add support for additional credential types.";
            log.Error(unsupportedCredentialsError);
            throw new ArgumentException(unsupportedCredentialsError);
        }

        public ApplicationCredentialsDTO AuthenticateUserToOpenStack(AuthenticationCredentials credentials)
        {
            if (credentials is not OpenIdCredentials openIdCredentials)
            {
                string unsupportedCredentialsError = $"Credentials of class {credentials.GetType().Name} are not supported.";
                log.Error(unsupportedCredentialsError);
                throw new ArgumentException(unsupportedCredentialsError);
            }
            var user = HandleOpenIdAuthentication(openIdCredentials);
            log.Info($"Keycloak user {user.Username} wants to authenticate to the OpenStack.");

            //var writeRole = unitOfWork.AdaptorUserRoleRepository.GetWriteRole();
            // Require write role.
            //if (!user.HasUserRole(writeRole))
            //{
            //    throw InsufficientRoleException.CreateMissingRoleException(writeRole, user.Roles);
            //}

            // OpenStack part.
            return CreateNewOpenStackSession(user);

        }

        /// <summary>
        /// Create new session/application credentials for authenticated user.
        /// </summary>
        /// <param name="userAccount">User with access to OpenStack part of the HEAppE.</param>
        /// <returns>OpenStack application credentials.</returns>
        /// <exception cref="AuthenticationException">is throws, if OpenStack service is inaccessible.</exception>
        private ApplicationCredentialsDTO CreateNewOpenStackSession(AdaptorUser userAccount)
        {
            OpenStackAuthenticationCredentials osInstanceCredentials = unitOfWork.OpenStackAuthenticationCredentialsRepository.GetDefaultAccount();

            var openStack = new OpenStack(osInstanceCredentials.OpenStackInstance.InstanceUrl);
            ApplicationCredentialsDTO openStackCredentials;
            try
            {
                var authResponse = openStack.Authenticate(osInstanceCredentials.Username, osInstanceCredentials.Password, osInstanceCredentials.OpenStackInstance.Domain, osInstanceCredentials.OpenStackInstance.Project);
                openStackCredentials = openStack.CreateApplicationCredentials(userAccount.Username, authResponse);
            }
            catch (OpenStackAPIException ex)
            {
                // Log the error and rethrow the exception.
                string error = $"Failed to retrieve OpenStack token for authorized Keycloak user: {userAccount.Username}. Reason: {ex.Message}";
                log.Error(error);
                throw new AuthenticationException("Unable to retrieve OpenStack application credentials for this user.");
            }

            StoreOpenStackSession(userAccount, openStackCredentials);
            log.Info($"Created new OpenStack 'session' (application credentials) for user {userAccount.Username}.");
            return openStackCredentials;
        }

        /// <summary>
        /// Check the provided OpenId tokens, refresh the token and the pass to logic to user retrieval.
        /// </summary>
        /// <param name="openIdCredentials">OpenId tokens.</param>
        /// <returns>New or existing user.</returns>
        private AdaptorUser HandleOpenIdAuthentication(OpenIdCredentials openIdCredentials)
        {
            //TODO at REST API validation
            if (string.IsNullOrWhiteSpace(openIdCredentials.OpenIdAccessToken))
            {
                string error = "Empty access_token in HandleOpenIdAuthentication.";
                log.Error(error);
                throw new OpenIdAuthenticationException(error);
            }

            try
            {
                KeycloakOpenId keycloak = new KeycloakOpenId();
                var tokenIntrospectResult = keycloak.TokenIntrospection(openIdCredentials.OpenIdAccessToken);
                KeycloakOpenId.ValidateUserToken(tokenIntrospectResult);

                string offline_token = keycloak.ExchangeToken(openIdCredentials.OpenIdAccessToken).AccessToken;
                var userInfo = keycloak.GetUserInfo(offline_token);

                return GetOrRegisterNewOpenIdUser(userInfo.Convert());
            }
            catch (KeycloakOpenIdException keycloakException)
            {
                log.Error($"OpenId: Failed to authenticate user via access token. access_token='{openIdCredentials.OpenIdAccessToken}'", keycloakException);
                throw new OpenIdAuthenticationException("Invalid or not active OpenId token provided. Unable to authenticate user by provided credentials.", keycloakException);
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

                log.Info($"Open-Id: User '{user.Username}' was added to group: '{keycloakGroup.Name}'");
            }

            if (!openIdUser.ProjectRoles.TryGetValue(KeycloakConfiguration.Project, out IEnumerable<string> openIdRoles))
            {
                throw new Exception("No roles for specific project is set");
            }

            var availableRoles = unitOfWork.AdaptorUserRoleRepository.GetAllByRoleNames(openIdRoles).ToList();
            var userRoles = unitOfWork.AdaptorUserRoleRepository.GetAllByUserId(user.Id).ToList();

            if (!(availableRoles.Count == userRoles.Count && availableRoles.Count == availableRoles.Intersect(userRoles).Count()))
            {
                user.AdaptorUserUserRoles = availableRoles.Select(s => new AdaptorUserUserRole
                {
                    AdaptorUserId = user.Id,
                    AdaptorUserRoleId = s.Id
                }).ToList();

                try
                {
                    unitOfWork.Save();
                    log.Info($"Open-Id: User '{user.Username}' has new roles: '{string.Join(",", availableRoles.Select(role => role.Name)) ?? "None"}' old roles were: '{string.Join(",", userRoles.Select(role => role.Name)) ?? "None"}'");
                }
                catch (Exception e)
                {
                    log.Error("Failed to set user roles.", e);
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
                AdaptorUser userAccount = unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
                if (userAccount is null)
                {
                    // Create new Keycloak user account.
                    userAccount = new AdaptorUser
                    {
                        Username = openIdUser.UserName,
                        Deleted = false,
                        Synchronize = false,
                    };

                    unitOfWork.AdaptorUserRepository.Insert(userAccount);
                    unitOfWork.Save();
                    log.Info($"Open-Id: Created new HEAppE account for Keycloak OpenId user: '{userAccount}'");
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
            userGroup = unitOfWork.AdaptorUserGroupRepository.GetGroupByUniqueName(groupName);
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
                unitOfWork.Save();
                return true;
            }
            catch (Exception e)
            {
                log.Error("Failed to add user to group.", e);
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

                log.Error("Authentication of user " + user.Username + " was not successful due to wrong credentials.");
                throw new InvalidAuthenticationCredentialsException("Authentication of user " + user.Username +
                                                                    " was not successful due to wrong credentials.");
            }
        }

        private string AuthenticateUserWithPassword(AdaptorUser user, PasswordCredentials credentials)
        {
            //get the bytes
            byte[] hashBytes = Convert.FromBase64String(user.Password);
            //extract salt
            byte[] salt = new byte[cSaltBytes];
            Array.Copy(hashBytes, 0, salt, 0, cSaltBytes);
            //create password hash
            var pbkdf2 = new Rfc2898DeriveBytes(credentials.Password, salt);
            byte[] hash = pbkdf2.GetBytes(cHashBytes);
            //verify password
            for (int i = 0; i < cHashBytes; i++)
            {
                if (hashBytes[i + cSaltBytes] != hash[i])
                {
                    log.Error("Authentication of user " + user.Username + " was not successful due to wrong credentials.");
                    throw new InvalidAuthenticationCredentialsException("Authentication of user " + user.Username +
                                                                        " was not successful due to wrong credentials.");
                }
            }

            return CreateSessionCode(user).UniqueCode;
        }

        public IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser)
        {
            IList<SubmittedJobInfo> notFinishedJobs = LogicFactory.GetLogicFactory()
                                                                  .CreateJobManagementLogic(unitOfWork)
                                                                  .ListNotFinishedJobInfosForSubmitterId(loggedUser.Id);
            IList<ClusterNodeType> nodeTypes = LogicFactory.GetLogicFactory()
                                                           .CreateClusterInformationLogic(unitOfWork)
                                                           .ListClusterNodeTypes();
            IList<ResourceUsage> result = new List<ResourceUsage>(nodeTypes.Count);
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
            return loggedUser.Groups.FirstOrDefault() ?? unitOfWork.AdaptorUserGroupRepository.GetDefaultSubmitterGroup();
        }

        public bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo)
        {
            return jobInfo.Submitter.Id == loggedUser.Id;
        }

        private bool IsSessionExpired(SessionCode session)
        {
            return session.LastAccessTime < DateTime.UtcNow.AddSeconds(-SessionExpirationSeconds);
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
            unitOfWork.OpenStackSessionRepository.Insert(session);
            unitOfWork.Save();
        }

        private SessionCode CreateSessionCode(AdaptorUser user)
        {
            SessionCode code = unitOfWork.SessionCodeRepository.GetByUser(user);

            if (!(code == null || IsSessionExpired(code)))
            {
                // User already has session, return it
                // Update auth time to current time and update db
                code.LastAccessTime = DateTime.UtcNow;
                unitOfWork.SessionCodeRepository.Update(code);
                unitOfWork.Save();
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
            } while (unitOfWork.SessionCodeRepository.GetByUniqueCode(guid.ToString()) != null);

            // Put it into db
            SessionCode sessionCode = new SessionCode
            {
                AuthenticationTime = DateTime.UtcNow,
                LastAccessTime = DateTime.UtcNow,
                UniqueCode = guid.ToString(),
                User = user
            };
            unitOfWork.SessionCodeRepository.Insert(sessionCode);
            unitOfWork.Save();
            return sessionCode;
        }
    }
}
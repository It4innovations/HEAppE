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

            var writeRole = unitOfWork.AdaptorUserRoleRepository.GetWriteRole();
            // Require write role.
            if (!user.HasUserRole(writeRole))
            {
                throw InsufficientRoleException.CreateMissingRoleException(writeRole, user.Roles);
            }

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
            OpenStackAuthenticationCredentials osInstanceCredentials =
                unitOfWork.OpenStackAuthenticationCredentialsRepository.GetDefaultAccount();

            OpenStack openStack = new OpenStack(new OpenStack.OpenStackInfo
            {
                OpenStackUrl = osInstanceCredentials.OpenStackInstance.InstanceUrl,
                Domain = osInstanceCredentials.OpenStackInstance.Domain,
                ServiceAccUsername = osInstanceCredentials.Username,
                ServiceAccPassword = osInstanceCredentials.Password
            });

            string uniqueTokenName = userAccount.Username + Guid.NewGuid();

            ApplicationCredentialsDTO openStackCredentials;
            DateTime sessionExpiration = DateTime.UtcNow.AddSeconds(SessionExpirationSeconds);
            try
            {
                openStackCredentials = openStack.CreateApplicationCredentials(uniqueTokenName, sessionExpiration);
            }
            catch (OpenStackAPIException ex)
            {
                // Log the error and rethrow the exception.
                string error =
                    $"Failed to retrieve OpenStack token for authorized Keycloak user: {userAccount.Username}. Reason: {ex.Message}";
                log.Error(error);
                throw new AuthenticationException("Unable to retrieve OpenStack application credentials for this user.");
            }

            StoreOpenStackSession(userAccount, openStackCredentials, sessionExpiration);
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
            if (string.IsNullOrWhiteSpace(openIdCredentials.OpenIdAccessToken))
            {
                string error = "Empty access_token in HandleOpenIdAuthentication.";
                log.Error(error);
                throw new OpenIdAuthenticationException(error);
            }

            KeycloakOpenId keycloak = new KeycloakOpenId();
            try
            {
                var tokenIntrospectResult = keycloak.TokenIntrospection(openIdCredentials.OpenIdAccessToken);
                keycloak.ValidateUserToken(tokenIntrospectResult);

                var tt = keycloak.ExchangeToken(openIdCredentials.OpenIdAccessToken);
                string offline_token = tt.AccessToken;

                var userInfo = keycloak.GetUserInfo(offline_token);

                var username = keycloak.CreateOpenIdUsernameForHEAppE(userInfo);
                return GetOrRegisterNewOpenIdUser("");
            }
            catch (KeycloakOpenIdException keycloakException)
            {
                log.Error($"Failed to refresh OpenId token. access_token='{openIdCredentials.OpenIdAccessToken}'", keycloakException);
                throw new OpenIdAuthenticationException("Invalid OpenId tokens provided. Unable to refresh provided keycloak credentials.", keycloakException);
            }
        }

        private void SynchonizeKeycloakUserGroupAndRoles(AdaptorUser user)//, DecodedAccessToken decodedAccessToken)
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
                log.Info($"User '{user.Username}' was added to group: '{keycloakGroup.Name}'");
            }

           List<AdaptorUserRole> userRoles = new List<AdaptorUserRole>(3);
            //if (decodedAccessToken.CanListProject(keycloakGroup.AccountingString))
            //{
            //    userRoles.Add(unitOfWork.AdaptorUserRoleRepository.GetListRole());
            //}
            //if (decodedAccessToken.CanReadProject(keycloakGroup.AccountingString))
            //{
            //    userRoles.Add(unitOfWork.AdaptorUserRoleRepository.GetReadRole());
            //}
            //if (decodedAccessToken.CanWriteProject(keycloakGroup.AccountingString))
            //{
            //    userRoles.Add(unitOfWork.AdaptorUserRoleRepository.GetWriteRole());
            //}
            //bool differentRoles = user.AdaptorUserUserRoles.Count != userRoles.Count;
            //if (!SetUserRoles(user, userRoles))
            //{
            //    throw new OpenIdAuthenticationException("Failed to set user roles. Check the log for details.");
            //}
            //if (differentRoles)
            //{
            //    log.Info($"User '{user.Username}' has new roles: '{string.Join(",", userRoles.Select(role => role.Name))}'");
            //}

        }

        /// <summary>
        /// Get existing or create new HEAppE user, from the OpenId credentials.
        /// </summary>
        /// <param name="decodedAccessToken">Decoded OpenId access token.</param>
        /// <returns>Newly created or existing HEAppE account.</returns>
        private AdaptorUser GetOrRegisterNewOpenIdUser(string openIdUserName)
        {
            lock (_lockCreateUserObj)
            {
                AdaptorUser userAccount = unitOfWork.AdaptorUserRepository.GetByName(openIdUserName);
                if (userAccount is not null)
                {
                    SynchonizeKeycloakUserGroupAndRoles(userAccount);
                    return userAccount;
                }
                else
                {
                    // Create new Keycloak user account.
                    AdaptorUser newKeycloakOpenIdUser = new AdaptorUser
                    {
                        Username = openIdUserName,
                        Deleted = false,
                        Synchronize = false,
                    };
                    unitOfWork.AdaptorUserRepository.Insert(newKeycloakOpenIdUser);
                    unitOfWork.Save();
                    log.Info($"Created new HEAppE account for Keycloak OpenId user: {openIdUserName}");


                    AdaptorUser user = unitOfWork.AdaptorUserRepository.GetByName(openIdUserName);
                    System.Diagnostics.Debug.Assert(user is not null, "User was just created and can't be null");
                    SynchonizeKeycloakUserGroupAndRoles(user);
                    return user;
                }
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

        /// <summary>
        /// Set new user roles. This will remove any role not present in the <paramref name="userRoles"/> list.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="userRoles">User roles.</param>
        /// <returns>True if user roles were saved.</returns>
        private bool SetUserRoles(AdaptorUser user, List<AdaptorUserRole> userRoles)
        {
            user.AdaptorUserUserRoles = userRoles.Select(role => new AdaptorUserUserRole
            {
                AdaptorUserId = user.Id,
                AdaptorUserRoleId = role.Id
            }).ToList();

            try
            {
                unitOfWork.Save();
            }
            catch (Exception e)
            {
                log.Error("Failed to set user roles.", e);
                return false;
            }
            return true;
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
        private void StoreOpenStackSession(AdaptorUser user, ApplicationCredentialsDTO applicationCredentials, DateTime expiresAt)
        {
            OpenStackSession session = new OpenStackSession
            {
                UserId = user.Id,
                AuthenticationTime = DateTime.UtcNow,
                ExpirationTime = expiresAt,
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public class UserAndLimitationManagementService : IUserAndLimitationManagementService
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Cache provider
        /// </summary>
        private readonly IMemoryCache _cacheProvider;
        #endregion

        public UserAndLimitationManagementService(IMemoryCache memoryCache)
        {
            _cacheProvider = memoryCache;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public async Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials)
        {
            try
            {
                AuthenticationCredentials credentialsIn;
                if (credentials is PasswordCredentialsExt)
                {
                    credentialsIn = new PasswordCredentials
                    {
                        Username = credentials.Username,
                        Password = ((PasswordCredentialsExt)credentials).Password
                    };
                }
                else if (credentials is DigitalSignatureCredentialsExt)
                {
                    credentialsIn = new DigitalSignatureCredentials
                    {
                        Username = credentials.Username,
                        DigitalSignature = Array.ConvertAll(((DigitalSignatureCredentialsExt)credentials).DigitalSignature, b => unchecked((byte)b)),
                        SignedContent = CombineContentWithSalt(credentials.Username)
                    };
                }
                else if (credentials is OpenIdCredentialsExt openIdCredentials)
                {
                    //Username is extracted from the access_token later.
                    credentialsIn = new OpenIdCredentials
                    {
                        OpenIdAccessToken = openIdCredentials.OpenIdAccessToken,
                    };
                }
                else
                {
                    _log.Error("Credentials of class " + credentials.GetType().Name +
                              " are not supported. Change the HaaSMiddleware.ServiceTier.UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.");
                    throw new ArgumentException("Credentials of class " + credentials.GetType().Name +
                                                " are not supported. Change the HaaSMiddleware.ServiceTier.UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.");
                }

                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    IUserAndLimitationManagementLogic userLogic =
                        LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    var result = await userLogic.AuthenticateUserAsync(credentialsIn);
                    return result;
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public async Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials)
        {
            if (credentials is OpenIdCredentialsExt openIdCredentials)
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    AdaptorUser user = await userLogic.AuthenticateUserToOpenStackAsync(new OpenIdCredentials
                    {
                        OpenIdAccessToken = openIdCredentials.OpenIdAccessToken
                    });

                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {   user.Id.ToString(),
                            nameof(AuthenticateUserToOpenStackAsync)
                        }
                    );

                    if (_cacheProvider.TryGetValue(memoryCacheKey, out OpenStackApplicationCredentialsExt value))
                    {
                        _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                        return value;
                    }
                    else
                    {
                        _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                        var appCreds = await userLogic.AuthenticateKeycloakUserToOpenStackAsync(user);
                        _cacheProvider.Set(memoryCacheKey, appCreds.ConvertIntToExt(), TimeSpan.FromSeconds(OpenStackSettings.OpenStackSessionExpiration));
                        return appCreds.ConvertIntToExt();
                    }
                }
            }
            else
            {
                string errorMessage = $"Credentials of type {credentials.GetType().Name} are not supported for OpenStack authentication.";
                _log.Error(errorMessage);
                throw new ArgumentException(errorMessage);
            }
        }

        public IEnumerable<ResourceUsageExt> GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    return userLogic.GetCurrentUsageAndLimitationsForUser(loggedUser).Select(s=>s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public bool ValidateUserPermissions(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator);
                    return loggedUser is not null;
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return false;
            }
        }

        /// <summary>
        /// Get user for given <paramref name="sessionCode"/> and check if the user has <paramref name="requiredUserRole"/>.
        /// </summary>
        /// <param name="sessionCode">User session code.</param>
        /// <param name="unitOfWork">Unit of work.</param>
        /// <param name="requiredUserRole">Required user role.</param>
        /// <returns>AdaptorUser object if user has required user role.</returns>
        /// <exception cref="InsufficientRoleException">Is thrown if the user doesn't have <paramref name="requiredUserRole"/>.</exception>
        public static AdaptorUser GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork, UserRoleType requiredUserRole)
        {
            IUserAndLimitationManagementLogic authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

            CheckUserRole(loggedUser, requiredUserRole);

            return loggedUser;
        }

        /// <summary>
        /// Check whether the user has any of the allowed roles to access given functionality.
        /// </summary>
        /// <param name="user">User account with roles.</param>
        /// <param name="requiredRole">Required user role.</param>
        /// <exception cref="InsufficientRoleException">is thrown when the user doesn't have any role specified by <see cref="allowedRoles"/></exception>
        internal static void CheckUserRole(AdaptorUser user, UserRoleType requiredUserRole)
        {
            bool hasRequiredRole = user.Roles.Any(userRole => (UserRoleType)userRole.Id == requiredUserRole);
            if (!hasRequiredRole)
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                var requiredRoleModel = unitOfWork.AdaptorUserRoleRepository.GetById((long)requiredUserRole);
                throw InsufficientRoleException.CreateMissingRoleException(requiredRoleModel, user.Roles);
            }
        }
        /// <summary>
        /// Check if user is in Group which is referenced to specific project
        /// </summary>
        /// <param name="user">User account</param>
        /// <param name="projectId">Project identifier</param>
        /// <exception cref="AdaptorUserNotReferencedForProjectException"></exception>
        public static void CheckUserProjectReference(AdaptorUser user, long projectId)
        {
            if (!user.Groups.Any(g => g.ProjectId == projectId))
            {
                throw new AdaptorUserNotReferencedForProjectException($"User {user.GetLogIdentification()} is not able to run job under ProjectId={projectId}");
            }
        }

        /// <summary>
        ///   Combines username with random salt.
        ///   Username is inserted into salt string on position
        ///   given by integer value of first character of the salt moduFlo length of the salt.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="salt">Salt</param>
        /// <returns>Combined string</returns>
        private string CombineContentWithSalt(string username)
        {
            string salt = GetRandomString();
            int val = (Encoding.UTF8.GetBytes(salt)[0]) % salt.Length;
            StringBuilder sb = new StringBuilder(salt);
            sb.Insert(val, username);
            return sb.ToString();
        }

        private static string GetRandomString()
        {
            var random = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(random);
            return Convert.ToBase64String(random);
        }
    }
}
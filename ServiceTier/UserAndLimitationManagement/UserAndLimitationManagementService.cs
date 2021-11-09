using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
using HEAppE.ServiceTier.UserAndLimitationManagement.Roles;
using log4net;

namespace HEAppE.ServiceTier.UserAndLimitationManagement
{
    public class UserAndLimitationManagementService : IUserAndLimitationManagementService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string AuthenticateUser(AuthenticationCredentialsExt credentials)
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
                    // NOTE(Moravec): We don't copy username, as it is not set yet. Username is extracted from the access_token later.
                    credentialsIn = new OpenIdCredentials
                    {
                        OpenIdAccessToken = openIdCredentials.OpenIdAccessToken,
                    };
                }
                else
                {
                    log.Error("Credentials of class " + credentials.GetType().Name +
                              " are not supported. Change the HaaSMiddleware.ServiceTier.UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.");
                    throw new ArgumentException("Credentials of class " + credentials.GetType().Name +
                                                " are not supported. Change the HaaSMiddleware.ServiceTier.UserAndLimitationManagement.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.");
                }

                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    IUserAndLimitationManagementLogic userLogic =
                        LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    var result = userLogic.AuthenticateUser(credentialsIn);
                    return result;
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public OpenStackApplicationCredentialsExt AuthenticateUserToOpenStack(AuthenticationCredentialsExt credentials)
        {
            if (credentials is OpenIdCredentialsExt openIdCredentials)
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    var appCreds = userLogic.AuthenticateUserToOpenStack(new OpenIdCredentials
                    {
                        OpenIdAccessToken = openIdCredentials.OpenIdAccessToken
                    });

                    return appCreds.ConvertIntToExt();
                }
            }

            string errorMessage = $"Credentials of type {credentials.GetType().Name} are not supported for OpenStack authentication.";
            log.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        public ResourceUsageExt[] GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = GetUserForSessionCode(sessionCode, unitOfWork);
                    IUserAndLimitationManagementLogic userLogic =
                        LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    IList<ResourceUsage> usages = userLogic.GetCurrentUsageAndLimitationsForUser(loggedUser);
                    return (from usage in usages select usage.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        /*public AdaptorUserGroupExt[] GetPossibleSubmitterGroupsForCurrentUser(string sessionCode) {
			throw new NotImplementedException();
		}*/

        internal static AdaptorUser GetUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork)
        {
            IUserAndLimitationManagementLogic authenticationLogic =
                LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);
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
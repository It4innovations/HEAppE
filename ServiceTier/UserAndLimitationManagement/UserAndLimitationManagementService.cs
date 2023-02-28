﻿using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
                        SignedContent = StringUtils.CombineContentWithSalt(credentials.Username)
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
                    var message = $"Credentials of class {credentials.GetType().Name} are not supported. Change the HEAppE.ServiceTier.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.";
                    _log.Error(message);
                    throw new ArgumentException(message);
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

        public async Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(AuthenticationCredentialsExt credentials, long projectId)
        {
            if (credentials is OpenIdCredentialsExt openIdCredentials)
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    AdaptorUser user = await userLogic.AuthenticateUserToOpenIdAsync(new OpenIdCredentials
                    {
                        OpenIdAccessToken = openIdCredentials.OpenIdAccessToken
                    });

                    string memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>()
                        {   user.Id.ToString(),
                            projectId.ToString(),
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
                        var appCreds = await userLogic.AuthenticateOpenIdUserToOpenStackAsync(user, projectId);
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
                    AdaptorUser loggedUser = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter, null);
                    IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    return userLogic.GetCurrentUsageAndLimitationsForUser(loggedUser).Select(s => s.ConvertIntToExt());
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
                    AdaptorUser loggedUser = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Administrator, null);
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
        /// <param name="projectId">Project ID (is is null, then test for all projects)</param>
        /// <returns>AdaptorUser object if user has required user role.</returns>
        /// <exception cref="InsufficientRoleException">Is thrown if the user doesn't have <paramref name="requiredUserRole"/>.</exception>
        public static AdaptorUser GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork, UserRoleType requiredUserRole, long? projectId)
        {
            IUserAndLimitationManagementLogic authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

            if (projectId.HasValue)
            {
                CheckUserRoleForProject(loggedUser, requiredUserRole, projectId.Value);
            }
            else if (ServiceTierSettings.SingleProjectId.HasValue)
            {
                CheckUserRoleForProject(loggedUser, requiredUserRole, ServiceTierSettings.SingleProjectId.Value);
            }
            else
            {
                loggedUser.Groups.Select(x => x.ProjectId).ToList().ForEach(projectId => CheckUserRoleForProject(loggedUser, requiredUserRole, projectId.Value));
            }

            return loggedUser;
        }

        /// <summary>
        /// Check whether the user has any of the allowed roles to access given functionality.
        /// </summary>
        /// <param name="user">User account with roles.</param>
        /// <param name="requiredUserRole">Required user role.</param>
        /// <param name="projectId">Project ID</param>
        /// <exception cref="InsufficientRoleException">is thrown when the user doesn't have any role specified by <see cref="allowedRoles"/></exception>
        private static void CheckUserRoleForProject(AdaptorUser user, UserRoleType requiredUserRole, long projectId)
        {
            bool hasRequiredRole = user.AdaptorUserUserGroupRoles.Any(x => (UserRoleType)x.AdaptorUserRoleId == requiredUserRole && x.AdaptorUserGroup.ProjectId == projectId && !x.AdaptorUserGroup.Project.IsDeleted && x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow);
            if (!hasRequiredRole)
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                var requiredRoleModel = unitOfWork.AdaptorUserRoleRepository.GetById((long)requiredUserRole);
                throw InsufficientRoleException.CreateMissingRoleException(requiredRoleModel, user.GetRolesForProject(projectId), projectId);
            }
        }
    }
}
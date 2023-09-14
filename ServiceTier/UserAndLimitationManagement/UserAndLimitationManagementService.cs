using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;
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
                else if (credentials is LexisCredentialsExt lexisCredentialsExt)
                {
                    //Username is extracted from the access_token later.
                    credentialsIn = new LexisCredentials
                    {
                        OpenIdLexisAccessToken = lexisCredentialsExt.OpenIdAccessToken,
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

        [Obsolete]
        public IEnumerable<ResourceUsageExt> GetCurrentUsageAndLimitationsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    return userLogic.GetCurrentUsageAndLimitationsForUser(loggedUser, projects).Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<ProjectResourceUsageExt> CurrentUsageAndLimitationsForCurrentUserByProject(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    return userLogic.CurrentUsageAndLimitationsForUserByProject(loggedUser, projects).Select(s => s.ConvertIntToExt());
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public IEnumerable<ProjectReferenceExt> ProjectsForCurrentUser(string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    (AdaptorUser loggedUser, var projects) = GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Reporter);
                    IUserAndLimitationManagementLogic userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                    return userLogic.ProjectsForCurrentUser(loggedUser, projects).Select(p => p.ConvertIntToExt());
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
                    AdaptorUser loggedUser = GetValidatedHpcProjectAdminUserForSessionCode(sessionCode, unitOfWork);
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
        public static AdaptorUser GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork, UserRoleType requiredUserRole, long projectId)
        {
            IUserAndLimitationManagementLogic authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

            CheckUserRoleForProject(loggedUser, requiredUserRole, projectId);
            return loggedUser;
        }

        /// <summary>
        /// Get user for given <paramref name="sessionCode"/> and check if the user has HpcProjectAdmin role
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <param name="unitOfWork"></param>
        /// <returns></returns>
        public static AdaptorUser GetValidatedHpcProjectAdminUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork)
        {
            IUserAndLimitationManagementLogic authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

            bool hasRequiredRole = loggedUser.AdaptorUserUserGroupRoles.Any(x => (UserRoleType)x.AdaptorUserRoleId == UserRoleType.HpcProjectAdmin);

            if (!hasRequiredRole)
            {
                var requiredRoleModel = unitOfWork.AdaptorUserRoleRepository.GetById((long)UserRoleType.HpcProjectAdmin);
                throw InsufficientRoleException.CreateMissingRoleException(requiredRoleModel);
            }
            return loggedUser;
        }

        public static (AdaptorUser, DomainObjects.JobManagement.Project[] projectIds) GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork, UserRoleType requiredUserRole)
        {
            IUserAndLimitationManagementLogic authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            AdaptorUser loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

            var projectIds = loggedUser.AdaptorUserUserGroupRoles.Where(x => (UserRoleType)x.AdaptorUserRoleId == requiredUserRole && !x.AdaptorUserGroup.Project.IsDeleted &&
                                                                                x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow &&
                                                                                !x.IsDeleted)
                                                                                .Select(y => y.AdaptorUserGroup.Project)
                                                                                .Distinct()
                                                                                .ToArray();
            return (loggedUser, projectIds); 
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
            bool hasRequiredRole = user.AdaptorUserUserGroupRoles.Any(x => (UserRoleType)x.AdaptorUserRoleId == requiredUserRole && x.AdaptorUserGroup.ProjectId == projectId && !x.AdaptorUserGroup.Project.IsDeleted && x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow && !x.IsDeleted);
            if (!hasRequiredRole)
            {
                using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
                var project = unitOfWork.ProjectRepository.GetById(projectId);
                if (project is null || project.IsDeleted)
                {
                    throw new InsufficientRoleException($"Project with ID {projectId} does not exist or was deleted.");
                }
                var requiredRoleModel = unitOfWork.AdaptorUserRoleRepository.GetById((long)requiredUserRole);
                throw InsufficientRoleException.CreateMissingRoleException(requiredRoleModel, user.GetRolesForProject(projectId).Distinct(), projectId);
            }
        }
    }
}
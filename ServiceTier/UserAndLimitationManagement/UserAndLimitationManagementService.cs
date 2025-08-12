using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;

namespace HEAppE.ServiceTier.UserAndLimitationManagement;

public class UserAndLimitationManagementService : IUserAndLimitationManagementService
{
    public UserAndLimitationManagementService(IMemoryCache memoryCache)
    {
        _cacheProvider = memoryCache;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    public async Task<string> AuthenticateUserAsync(AuthenticationCredentialsExt credentials)
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
                DigitalSignature = Array.ConvertAll(((DigitalSignatureCredentialsExt)credentials).DigitalSignature,
                    b => unchecked((byte)b)),
                SignedContent = StringUtils.CombineContentWithSalt(credentials.Username)
            };
        }
        else if (credentials is OpenIdCredentialsExt openIdCredentials)
        {
            //Username is extracted from the access_token later.
            credentialsIn = new OpenIdCredentials
            {
                OpenIdAccessToken = openIdCredentials.OpenIdAccessToken
            };
        }
        else if (credentials is LexisCredentialsExt lexisCredentialsExt)
        {
            //Username is extracted from the access_token later.
            credentialsIn = new LexisCredentials
            {
                OpenIdLexisAccessToken = lexisCredentialsExt.OpenIdAccessToken
            };
        }
        else
        {
            var message =
                $"Credentials of class {credentials.GetType().Name} are not supported. Change the HEAppE.ServiceTier.UserAndLimitationManagementService.AuthenticateUser() method to add support for additional credential types.";
            _log.Error(message);
            throw new ArgumentException(message);
        }

        var result = string.Empty;
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var userLogic =
                LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            result = await userLogic.AuthenticateUserAsync(credentialsIn);
            if (!string.IsNullOrEmpty(result))
            { 
                _log.Info($"User {credentials.Username} authenticated successfully.");
            }
        }
        return result;
    }

    public async Task<OpenStackApplicationCredentialsExt> AuthenticateUserToOpenStackAsync(
        AuthenticationCredentialsExt credentials, long projectId)
    {
        if (credentials is OpenIdCredentialsExt openIdCredentials)
            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
                var user = await userLogic.AuthenticateUserToOpenIdAsync(new OpenIdCredentials
                {
                    OpenIdAccessToken = openIdCredentials.OpenIdAccessToken
                });

                var memoryCacheKey = StringUtils.CreateIdentifierHash(
                    new List<string>
                    {
                        user.Id.ToString(),
                        projectId.ToString(),
                        nameof(AuthenticateUserToOpenStackAsync)
                    }
                );

                if (_cacheProvider.TryGetValue(memoryCacheKey, out OpenStackApplicationCredentialsExt value))
                {
                    _log.Info($"Using Memory Cache to get value for key: \"{memoryCacheKey}\"");
                    return value;
                }

                _log.Info($"Reloading Memory Cache value for key: \"{memoryCacheKey}\"");
                var appCreds = await userLogic.AuthenticateOpenIdUserToOpenStackAsync(user, projectId);
                _cacheProvider.Set(memoryCacheKey, appCreds.ConvertIntToExt(),
                    TimeSpan.FromSeconds(OpenStackSettings.OpenStackSessionExpiration));
                return appCreds.ConvertIntToExt();
            }

        throw new AuthenticationTypeException("OpenId-NotSupportedAuthentication", credentials.GetType().Name);
    }

    public IEnumerable<ProjectResourceUsageExt> CurrentUsageAndLimitationsForCurrentUserByProject(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, projects) =
                GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Reporter);
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            return userLogic.CurrentUsageAndLimitationsForUserByProject(loggedUser, projects)
                .Select(s => s.ConvertIntToExt());
        }
    }

    public IEnumerable<ProjectReferenceExt> ProjectsForCurrentUser(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, projects) =
                GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Reporter);
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            return userLogic.ProjectsForCurrentUser(loggedUser, projects).Select(p => p.ConvertIntToExt());
        }
    }

    public bool ValidateUserPermissions(string sessionCode, AdaptorUserRoleType requestedRole)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, _) = GetValidatedUserForSessionCode(sessionCode, unitOfWork, requestedRole);
            return loggedUser is not null;
        }
    }

    public AdaptorUserExt GetCurrentUserInfo(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
            var loggedUser = userLogic.GetUserForSessionCode(sessionCode);
            return loggedUser.ConvertIntToExt();
        }
    }

    /// <summary>
    ///     Get user for given <paramref name="sessionCode" /> and check if the user has <paramref name="requiredUserRole" />.
    /// </summary>
    /// <param name="sessionCode">User session code</param>
    /// <param name="unitOfWork">Unit of work</param>
    /// <param name="requiredUserRole">Allowed User role</param>
    /// <param name="projectId">
    ///     Project Id /param>
    ///     <returns>AdaptorUser object if user has required user role.</returns>
    ///     <exception cref="InsufficientRoleException">
    ///         Is thrown if the user doesn't have <paramref name="requiredUserRole" />
    ///         .
    ///     </exception>
    ///     <exception cref="RequestedObjectDoesNotExistException">is thrown when the specific project does not exist.</exception>
    public static AdaptorUser GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork,
        AdaptorUserRoleType requiredUserRole, long projectId, bool overrideProjectValidityCheck = false)
    {
        var authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
        var loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

        CheckUserRoleForProject(loggedUser, requiredUserRole, projectId, overrideProjectValidityCheck);
        return loggedUser;
    }

    public static (AdaptorUser, IEnumerable<Project> projectIds) GetValidatedUserForSessionCode(string sessionCode,
        IUnitOfWork unitOfWork, AdaptorUserRoleType allowedRole)
    {
        var authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);
        var loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

        var projectIds = loggedUser.AdaptorUserUserGroupRoles.Where(x =>
                x.AdaptorUserRole.ContainedRoleTypes.Any(a => a == allowedRole) &&
                x.AdaptorUserGroup.Project != null &&
                x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow)
            .Select(y => y.AdaptorUserGroup.Project);
        return (loggedUser, projectIds);
    }

    /// <summary>
    ///     Check whether the user has any of the allowed roles to access given functionality.
    /// </summary>
    /// <param name="user">User account with roles.</param>
    /// <param name="requiredUserRole">Allowed user role.</param>
    /// <param name="projectId">Project Id</param>
    /// <exception cref="InsufficientRoleException">
    ///     is thrown when the user doesn't have any role specified by
    ///     <see cref="requiredUserRole" />
    /// </exception>
    /// <exception cref="RequestedObjectDoesNotExistException">is thrown when the specific project does not exist.</exception>
    private static void CheckUserRoleForProject(AdaptorUser user, AdaptorUserRoleType requiredUserRole, long projectId,
        bool overrideProjectValidityCheck = false)
    {
        var hasRequiredRole = user.AdaptorUserUserGroupRoles.Any(x =>
            x.AdaptorUserRole != null &&
            x.AdaptorUserRole.ContainedRoleTypes != null &&
            x.AdaptorUserRole.ContainedRoleTypes.Any(a => a == requiredUserRole) &&
            x.AdaptorUserGroup != null &&
            x.AdaptorUserGroup.ProjectId == projectId &&
            x.AdaptorUserGroup.Project != null &&
            !x.AdaptorUserGroup.Project.IsDeleted &&
            (overrideProjectValidityCheck ||
             x.AdaptorUserGroup.Project.EndDate >= DateTime.UtcNow) &&
            !x.IsDeleted);
        if (!hasRequiredRole)
        {
            using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
            var project = unitOfWork.ProjectRepository.GetById(projectId);
            if (project is null || (!overrideProjectValidityCheck && project.EndDate < DateTime.UtcNow))
                throw new RequestedObjectDoesNotExistException("ProjectNotFound");

            throw new InsufficientRoleException("MissingRoleForProject", requiredUserRole.ToString(), projectId);
        }
    }

    public Task<object> GetVaultHealth()
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            return unitOfWork.ClusterAuthenticationCredentialsRepository.GetVaultHealth();
        }
    }

    #region Instances

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILog _log;

    /// <summary>
    ///     Cache provider
    /// </summary>
    private readonly IMemoryCache _cacheProvider;

    #endregion
}
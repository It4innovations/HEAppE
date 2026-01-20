using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using Microsoft.Extensions.Caching.Memory;
using log4net;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.Utils;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using SshCaAPI;

namespace HEAppE.ServiceTier.UserAndLimitationManagement;

public class UserAndLimitationManagementService : IUserAndLimitationManagementService
{
    private ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys = null;
    private readonly IUserOrgService _userOrgService = null;
    public UserAndLimitationManagementService(IMemoryCache memoryCache, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _sshCertificateAuthorityService = sshCertificateAuthorityService ?? throw new ArgumentNullException(nameof(sshCertificateAuthorityService));
        _httpContextKeys = httpContextKeys ?? throw new ArgumentNullException(nameof(httpContextKeys));
        _cacheProvider = memoryCache;
        _userOrgService = userOrgService;
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
                LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
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
                var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
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
                    _log.Info($"Using Memory Cache to get value for key.");
                    return value;
                }

                _log.Info($"Reloading Memory Cache value for key.");
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
                GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys, AdaptorUserRoleType.Reporter);
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            return userLogic.CurrentUsageAndLimitationsForUserByProject(loggedUser, projects)
                .Select(s => s.ConvertIntToExt());
        }
    }

    public IEnumerable<ProjectReferenceExt> ProjectsForCurrentUser(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, projects) =
                GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys, AdaptorUserRoleType.Reporter);
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
            return userLogic.ProjectsForCurrentUser(loggedUser, projects).Select(p => p.ConvertIntToExt());
        }
    }

    public bool ValidateUserPermissions(string sessionCode, AdaptorUserRoleType requestedRole)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var (loggedUser, _) = GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys, requestedRole);
            return loggedUser is not null;
        }
    }

    public AdaptorUserExt GetCurrentUserInfo(string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys);
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
    public static AdaptorUser GetValidatedUserForSessionCode(string sessionCode, IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, 
        AdaptorUserRoleType requiredUserRole, long projectId, bool overrideProjectValidityCheck = false)
    {
        var authenticationLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, userOrgService,sshCertificateAuthorityService, httpContextKeys);
        
        if (JwtTokenIntrospectionConfiguration.IsEnabled || LexisAuthenticationConfiguration.UseBearerAuth)
        {
            if (httpContextKeys.Context.AdaptorUserId < 0)
            {
                throw new UnauthorizedAccessException("Unauthorized"); 
            }
            return authenticationLogic.GetUserById(httpContextKeys.Context.AdaptorUserId);
        }
        if(string.IsNullOrEmpty(sessionCode) && httpContextKeys.Context.AdaptorUserId > 0)
        {
            return authenticationLogic.GetUserById(httpContextKeys.Context.AdaptorUserId);
        }
        var loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);

        CheckUserRoleForProject(loggedUser, requiredUserRole, projectId, overrideProjectValidityCheck);
        return loggedUser;
    }

    public static (AdaptorUser, IEnumerable<Project> projects) GetValidatedUserForSessionCode(
        string sessionCode, IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, AdaptorUserRoleType allowedRole)
    {
        var authenticationLogic = LogicFactory.GetLogicFactory()
            .CreateUserAndLimitationManagementLogic(unitOfWork, userOrgService, sshCertificateAuthorityService, httpContextKeys);
        AdaptorUser loggedUser;

        if (JwtTokenIntrospectionConfiguration.IsEnabled || LexisAuthenticationConfiguration.UseBearerAuth)
        {
            if (httpContextKeys.Context.AdaptorUserId >= 0)
            {
                loggedUser = authenticationLogic.GetUserById(httpContextKeys.Context.AdaptorUserId);
                
            }
            else
            {
                throw new UnauthorizedAccessException("Unauthorized"); 
            }
        }
        else
        {
            if(string.IsNullOrEmpty(sessionCode) && httpContextKeys.Context.AdaptorUserId > 0)
            {
                loggedUser = authenticationLogic.GetUserById(httpContextKeys.Context.AdaptorUserId);
            }
            else
                loggedUser = authenticationLogic.GetUserForSessionCode(sessionCode);
        }
        
        if (loggedUser == null)
            throw new UnauthorizedAccessException("Unauthorized");
            

        var now = DateTime.UtcNow;

        var projects = loggedUser.AdaptorUserUserGroupRoles
            .Where(r =>
                r.AdaptorUserGroup.Project != null &&
                r.AdaptorUserGroup.Project.EndDate > now &&
                r.AdaptorUserRole.ContainedRoleTypes.Contains(allowedRole)
            )
            .Select(r => r.AdaptorUserGroup.Project)
            .Distinct()
            .ToList();

        return (loggedUser, projects);
    }

    
    public static (AdaptorUser User, IEnumerable<Project> Projects) GetValidatedUserForSessionCode(
        string sessionCode,
        IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, 
        IHttpContextKeys httpContextKeys,
        List<AdaptorUserRoleType> allowedRoles)
    {
        var authenticationLogic = LogicFactory.GetLogicFactory()
            .CreateUserAndLimitationManagementLogic(unitOfWork, userOrgService, sshCertificateAuthorityService, httpContextKeys);

        var user = authenticationLogic.GetUserForSessionCode(sessionCode);
        if (user == null)
            throw new UnauthorizedAccessException("Unauthorized");

        var now = DateTime.UtcNow;

        var projects = user.AdaptorUserUserGroupRoles
            .Where(role =>
                role.AdaptorUserGroup.Project != null &&
                role.AdaptorUserGroup.Project.EndDate > now &&
                role.AdaptorUserRole.ContainedRoleTypes
                    .Any(roleType => allowedRoles.Contains(roleType))
            )
            .Select(role => role.AdaptorUserGroup.Project)
            .Distinct()
            .ToList();


        return (user, projects);
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
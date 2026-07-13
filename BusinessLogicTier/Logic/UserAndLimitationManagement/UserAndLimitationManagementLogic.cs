using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.ExternalAuthentication.KeyCloak;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.OpenStackAPI;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using HEAppE.Utils;
using SshCaAPI;


namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;

public class UserAndLimitationManagementLogic : IUserAndLimitationManagementLogic
{
    #region Constructors

    internal UserAndLimitationManagementLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, 
                                              IHttpContextKeys httpContextKeys, IExpirioService expirioService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _logger = logger;
        _userOrgService = userOrgService;
        _expirioService = expirioService;
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Unit of work
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    ///     Logger
    /// </summary>
    private readonly ILogger _logger;

    private readonly IUserOrgService _userOrgService;

    /// <summary>
    /// Expirio service
    /// </summary>
    private readonly IExpirioService _expirioService;

    /// <summary>
    ///     Session code expiration in seconds
    /// </summary>
    private static readonly int _sessionExpirationSeconds = BusinessLogicConfiguration.SessionExpirationInSeconds;
    
    private ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    
    #endregion

    #region Methods

    public AdaptorUser GetUserForSessionCode(string sessionCode)
    {
        bool hasIdpOrLEXISToken = !string.IsNullOrEmpty(_httpContextKeys.Context.IdpToken) 
                                  || !string.IsNullOrEmpty(_httpContextKeys.Context.LEXISToken);

        if (!hasIdpOrLEXISToken && !string.IsNullOrEmpty(sessionCode))
        {
            _logger.LogInformation("Authenticating local user with session code.");
            return AuthenticateLocalSession(sessionCode);
        }

        if (_httpContextKeys.Context.AdaptorUserId != 0)
        {
            var cache = (IMemoryCache)LogicFactory.ServiceProvider?.GetService(typeof(IMemoryCache));
            if (cache != null)
            {
                string userCacheKey = $"UserById_{_httpContextKeys.Context.AdaptorUserId}";
                if (cache.TryGetValue(userCacheKey, out AdaptorUser cachedUser))
                {
                    _logger.LogDebug("Returning cached user for ID {UserId}", _httpContextKeys.Context.AdaptorUserId);
                    return cachedUser;
                }
            }

            var user = _unitOfWork.AdaptorUserRepository.GetById(_httpContextKeys.Context.AdaptorUserId);

            if (cache != null && user != null)
            {
                string userCacheKey = $"UserById_{_httpContextKeys.Context.AdaptorUserId}";
                cache.Set(userCacheKey, user, TimeSpan.FromSeconds(10));
            }
            return user;
        }

        return AuthenticateLocalSession(sessionCode);
    }

    private AdaptorUser AuthenticateLocalSession(string sessionCode)
    {
        var cache = (IMemoryCache)LogicFactory.ServiceProvider?.GetService(typeof(IMemoryCache));
        if (cache != null)
        {
            string sessionCacheKey = $"SessionUser_{sessionCode}";
            if (cache.TryGetValue(sessionCacheKey, out (AdaptorUser User, DateTime ExpirationTime) cached))
            {
                if (cached.ExpirationTime > DateTime.UtcNow)
                {
                    return cached.User;
                }
            }
        }

        var session = _unitOfWork.SessionCodeRepository.GetByUniqueCode(sessionCode);
        if (session is null)
            throw new UnauthorizedAccessException("Unauthorized");

        if (IsSessionExpired(session))
            throw new SessionCodeNotValidException(
                "Expired", 
                sessionCode,
                session.LastAccessTime.AddSeconds(_sessionExpirationSeconds).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            );

        var now = DateTime.UtcNow;
        if (session.LastAccessTime < now.AddSeconds(-30))
        {
            bool shouldUpdate = true;
            if (cache != null)
            {
                string updateCacheKey = $"SessionLastDbUpdate_{sessionCode}";
                if (cache.TryGetValue(updateCacheKey, out _))
                {
                    shouldUpdate = false;
                }
                else
                {
                    cache.Set(updateCacheKey, true, TimeSpan.FromSeconds(30));
                }
            }

            if (shouldUpdate)
            {
                session.LastAccessTime = now;
                _unitOfWork.SessionCodeRepository.Update(session);
                _unitOfWork.Save();
            }
        }

        var user = session.User;
        if (cache != null && user != null)
        {
            string sessionCacheKey = $"SessionUser_{sessionCode}";
            var expirationTime = session.LastAccessTime.AddSeconds(_sessionExpirationSeconds);
            var cacheExpiration = DateTime.UtcNow.AddSeconds(10);
            var finalExpiration = expirationTime < cacheExpiration ? expirationTime : cacheExpiration;
            cache.Set(sessionCacheKey, (user, finalExpiration), TimeSpan.FromSeconds(10));
        }

        return user;
    }
    
    public AdaptorUser GetUserById(long id)
    {
        return _unitOfWork.AdaptorUserRepository.GetById(id);
    }

    public async Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials)
    {
        switch (credentials)
        {
            case PasswordCredentials passwordCredentials:
                return AuthenticateUserWithPassword(passwordCredentials);
            case DigitalSignatureCredentials digitalSignatureCredentials:
                return AuthenticateUserWithDigitalSignature(digitalSignatureCredentials);
            case OpenIdCredentials openIdCredentials:
                var openIdUser = await HandleOpenIdAuthenticationAsync(openIdCredentials);
                credentials.Username = openIdUser.Username;
                return CreateSessionCode(openIdUser).UniqueCode;
            case LexisCredentials lexisCredentials:
                var lexisUser = await HandleTokenAsApiKeyAuthenticationAsync(lexisCredentials);
                credentials.Username = lexisUser.Username;
                return CreateSessionCode(lexisUser).UniqueCode;
            default:
                throw new AuthenticationTypeException("NotSupportedAuthentication");
        }
    }

    public async Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials)
    {
        _logger.LogInformation("OpenId: Authenticating user to the OpenStack using token.");

        var user = await HandleOpenIdAuthenticationAsync(credentials);
        return user;
    }

    /// <summary>
    ///     Create new session/application credentials for authenticated user.
    /// </summary>
    /// <param name="adaptorUser">User with access to OpenStack part of the HEAppE.</param>
    /// <param name="projectId">Project Id</param>
    /// <returns>OpenStack application credentials.</returns>
    /// <exception cref="AuthenticationException">is throws, if OpenStack service is inaccessible.</exception>
    public async Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser,
        long projectId)
    {
        try
        {
            _logger.LogInformation(
                $"OpenId: user \"{adaptorUser.Username}\" wants to authenticate to the OpenStack project \"{projectId}\".");

            if (!adaptorUser.Groups.Any(f => f.ProjectId == projectId))
                throw new AuthenticationTypeException("OpenStack-MissingCreateCredentialsPermission", projectId);

            var openStackProject = GetOpenStackInstanceWithProjects(projectId);
            var hasRequiredRole = adaptorUser.AdaptorUserUserGroupRoles.Any(x =>
                (AdaptorUserRoleType)x.AdaptorUserRoleId == AdaptorUserRoleType.Submitter
                && x.AdaptorUserGroup.ProjectId == openStackProject.HEAppEProjectId
                && x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow);

            if (!hasRequiredRole)
                throw new InsufficientRoleException("MissingRoleForProject", AdaptorUserRoleType.Submitter.ToString(),
                    openStackProject.HEAppEProjectId.Value);

            OpenStack openStack = new(openStackProject.Domain.InstanceUrl);
            var authResponse = await openStack.AuthenticateAsync(openStackProject);
            var openStackCredentials =
                await openStack.CreateApplicationCredentialsAsync(adaptorUser.Username, authResponse);

            OpenStackSession openStackSession = new()
            {
                UserId = adaptorUser.Id,
                AuthenticationTime = DateTime.UtcNow,
                ExpirationTime = openStackCredentials.ExpiresAt,
                ApplicationCredentialsId = openStackCredentials.ApplicationCredentialsId,
                ApplicationCredentialsSecret = openStackCredentials.ApplicationCredentialsSecret
            };

            _unitOfWork.OpenStackSessionRepository.Insert(openStackSession);
            _unitOfWork.Save();

            _logger.LogInformation(
                $"Created new OpenStack 'session' (application credentials) for user \"{adaptorUser.Username}\".");
            return openStackCredentials;
        }
        catch (AuthenticationTypeException)
        {
            throw new AuthenticationTypeException("OpenStack-UnableToRetrieveCredentials");
        }
    }

    public AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser, long projectId)
    {
        return loggedUser.Groups.Where(x => x.ProjectId == projectId).FirstOrDefault() ??
               loggedUser.Groups.FirstOrDefault() ?? _unitOfWork.AdaptorUserGroupRepository.GetDefaultSubmitterGroup();
    }

    public bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo, bool isAdminOverride = false)
    {
        if (isAdminOverride || (loggedUser.AdaptorUserUserGroupRoles?.Any(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Administrator) ?? false))
        {
            return true;
        }
        return jobInfo.Submitter.Id == loggedUser.Id;
    }

    public bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo, bool checkSharedJobInfoAccess = false)
    {
        if (loggedUser.AdaptorUserUserGroupRoles?.Any(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Administrator) ?? false)
        {
            return true;
        }
        bool isOwner = taskInfo.Specification.JobSpecification.Submitter.Id == loggedUser.Id;
        if (isOwner) 
            return true;
        if (!checkSharedJobInfoAccess || taskInfo.Project == null) 
            return false;
        var project = _unitOfWork.ProjectRepository.GetById(taskInfo.Project.Id);
        return project is { IsOneToOneMapping: false };
    }

    public IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser,
        IEnumerable<Project> projects)
    {
        var notFinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .GetNotFinishedJobInfosForSubmitterId(loggedUser.Id);
        var nodeTypes = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, _logger)
            .ListClusterNodeTypes();

        IList<ResourceUsage> result = new List<ResourceUsage>(nodeTypes.Count());
        foreach (var nodeType in nodeTypes)
        {
            ResourceUsage usage = new()
            {
                NodeType = nodeType,
                CoresUsed = notFinishedJobs.Sum(s => s.Tasks.Sum(taskSum => taskSum.Specification.MaxCores)) ?? 0
            };
            result.Add(usage);
        }

        return result;
    }

    public IList<ProjectResourceUsage> CurrentUsageAndLimitationsForUserByProject(AdaptorUser loggedUser,
        IEnumerable<Project> projects)
    {
        var allUserJobs = _unitOfWork.SubmittedJobInfoRepository.GetNotFinishedForSubmitterId(loggedUser.Id);
        var projectList = projects?.Where(p => p != null).ToList() ?? new List<Project>();

        IList<ProjectResourceUsage> result = new List<ProjectResourceUsage>();
        if (!projectList.Any())
        {
            return result;
        }

        var projectIds = projectList.Select(p => p.Id).Distinct().ToList();
        var templatesByProject = _unitOfWork.CommandTemplateRepository
            .GetCommandTemplatesByProjectIds(projectIds)
            .GroupBy(t => t.ProjectId)
            .ToDictionary(g => g.Key ?? 0, g => g.ToList());

        foreach (var project in projectList)
        {
            ProjectResourceUsage usage = new()
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

            if (templatesByProject.TryGetValue(project.Id, out var projectCommandTemplates))
            {
                var nodeTypes = projectCommandTemplates.Select(x => x.ClusterNodeType).Where(n => n != null).Distinct().ToList();
                foreach (var nodeType in nodeTypes)
                {
                    var tasksAtNode = allUserJobs.SelectMany(x => x.Tasks).Where(x => x.NodeType != null && x.NodeType.Id == nodeType.Id);
                    NodeUsedCoresAndLimitation clusterNodeUsedCoresAndLimitation = new()
                    {
                        CoresUsed = tasksAtNode.Sum(taskSum => taskSum.AllocatedCores) ?? 0,
                        NodeType = nodeType
                    };
                    ClusterNodeTypeResourceUsage clusterNodeTypeUsage = new()
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
            }

            result.Add(usage);
        }

        return result;
    }

    #endregion

    #region Private Methods

    private OpenStackProjectDTO GetOpenStackInstanceWithProjects(long? projectId)
    {
        var project = _unitOfWork.OpenStackProjectRepository.GetOpenStackProjectByProjectId(projectId.Value)
                      ?? throw new AuthenticationTypeException("OpenStack-NoOpenStackProject", projectId);

        var projectCredentials = project.OpenStackAuthenticationCredentialProjects.FirstOrDefault(f => f.IsDefault) ??
                                 project.OpenStackAuthenticationCredentialProjects.FirstOrDefault();

        return projectCredentials is null
            ? throw new AuthenticationTypeException("OpenStack-MissingCreateCredentialsPermission", projectId)
            : new OpenStackProjectDTO
            {
                Name = project.Name,
                UID = project.UID,
                HEAppEProjectId = project.AdaptorUserGroup.ProjectId,
                ProjectDomain = new OpenStackProjectDomainDTO
                {
                    UID = project.OpenStackProjectDomain.UID,
                    Name = project.OpenStackProjectDomain.Name
                },
                Domain = new OpenStackDomainDTO
                {
                    UID = project.OpenStackDomain.UID,
                    Name = project.OpenStackDomain.Name,
                    InstanceUrl = project.OpenStackDomain.OpenStackInstance.InstanceUrl
                },
                Credentials = new OpenStackCredentialsDTO
                {
                    Id = projectCredentials.OpenStackAuthenticationCredential.UserId,
                    Username = projectCredentials.OpenStackAuthenticationCredential.Username,
                    Password = projectCredentials.OpenStackAuthenticationCredential.Password
                }
            };
    }

    /// <summary>
    ///     Check the provided OpenId tokens, refresh the token and the pass to logic to user retrieval.
    /// </summary>
    /// <param name="openIdCredentials">OpenId credentials.</param>
    /// <returns>New or existing user.</returns>
    private async Task<AdaptorUser> HandleOpenIdAuthenticationAsync(OpenIdCredentials openIdCredentials)
    {
        try
        {
            KeycloakOpenId openIdClient = new();
            var tokenIntrospectResult = await openIdClient.TokenIntrospectionAsync(openIdCredentials.OpenIdAccessToken);
            KeycloakOpenId.ValidateUserToken(tokenIntrospectResult);
            var offline_token = (await openIdClient.ExchangeTokenAsync(openIdCredentials.OpenIdAccessToken))
                .AccessToken;
            var userInfo = await openIdClient.GetUserInfoAsync(offline_token);
            var userOpenId = userInfo.Convert();

            _logger.LogInformation($"OpenId: User \"{userOpenId.UserName}\" wants to authenticate to the system.");
            return GetOrRegisterNewOpenIdUser(userOpenId);
        }
        catch (AuthenticationTypeException)
        {
            throw new AuthenticationTypeException("InvalidToken");
        }
    }

    public async Task<AdaptorUser> HandleTokenAsApiKeyAuthenticationAsync(LexisCredentials lexisCredentials)
    {
        try
        {
            _logger.LogInformation("LEXIS AAI: Authenticating user using token.");
            string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
            var result = await _userOrgService.GetUserInfoAsync(lexisCredentials.OpenIdLexisAccessToken, instanceId, _logger);
            
            string username = result.UserName;
            if (string.IsNullOrEmpty(username))
            {
                username = !string.IsNullOrEmpty(result.KeycloakSid) ? result.KeycloakSid : result.Id.ToString();
            }
            if (string.IsNullOrEmpty(username))
            {
                username = StringUtils.GenerateUsername(result.Id.ToString());
            }

            _logger.LogInformation($"LEXIS AAI: User \"{username}\" wants to authenticate to the system.");
            return await GetOrRegisterLexisCredentialsAsync(result);
        }
        catch (HttpRequestException )
        {
            throw new AuthenticationTypeException("InvalidToken");
        }
    }
    
    

    /// <summary>
    ///     Get existing or create new HEAppE user, from the OpenId credentials.
    ///     Roles are synchronised to the DB only when they differ from the current state,
    ///     eliminating row-lock contention under concurrent bearer-token requests.
    ///     Uses a lean async DB query (groups + project only, no clusters/templates/users).
    /// </summary>
    private async Task<AdaptorUser> GetOrRegisterLexisCredentialsAsync(UserInfoExtendedModel lexisUser)
    {
        var lexisProjects = lexisUser.SystemRoles
            .Where(w => !string.IsNullOrEmpty(w.ProjectShortName))
            .Select(x => new
            {
                x.ProjectShortName,
                ProjectResourceNames = x.ProjectResources.Select(r => r.Name).Distinct(),
                Permissions = x.SystemPermissionTypes
            })
            .ToList();

        // Lean async query: filter by LEXIS prefix in SQL, join only Project — no clusters/templates/users
        var userLEXISGroups = await _unitOfWork.AdaptorUserGroupRepository
            .GetGroupsByPrefixWithActiveProjectsAsync(LexisAuthenticationConfiguration.HEAppEGroupNamePrefix);

        DateTime changedTime = DateTime.UtcNow;
        if (string.IsNullOrEmpty(lexisUser.Email))
        {
            throw new AuthenticationTypeException("MissingEmailInUserInfoFromUserOrg");
        }

        var idpSid = !string.IsNullOrEmpty(lexisUser.KeycloakSid) ? lexisUser.KeycloakSid : lexisUser.Id.ToString();
        AdaptorUser user = await _unitOfWork.AdaptorUserRepository.GetByIdpSidIgnoreQueryFiltersAsync(idpSid);
        
        string username = lexisUser.UserName;
        if (string.IsNullOrEmpty(username))
        {
            username = idpSid;
        }

        if (user is null)
        {
            // Fallback to match by email (non-breaking transition for existing accounts)
            user = await _unitOfWork.AdaptorUserRepository.GetByEmailIgnoreQueryFiltersAsync(lexisUser.Email);
            if (user != null)
            {
                user.IdpSid = idpSid;
                user = UpdateUser(user, user.Username, lexisUser.Email, changedTime, AdaptorUserType.Lexis);
            }
            else
            {
                try 
                {
                    user = CreateUser(username, lexisUser.Email, changedTime, AdaptorUserType.Lexis);
                    user.IdpSid = idpSid;
                    _unitOfWork.AdaptorUserRepository.Update(user);
                    _unitOfWork.Save();
                }
                catch (Exception)
                {
                    user = await _unitOfWork.AdaptorUserRepository.GetByIdpSidIgnoreQueryFiltersAsync(idpSid);
                    if (user is null)
                    {
                        user = await _unitOfWork.AdaptorUserRepository.GetByEmailIgnoreQueryFiltersAsync(lexisUser.Email);
                        if (user is null) throw;
                        
                        user.IdpSid = idpSid;
                        user = UpdateUser(user, user.Username, lexisUser.Email, changedTime, AdaptorUserType.Lexis);
                    }
                }
            }
        }
        else
        {
            user = UpdateUser(user, user.Username, lexisUser.Email, changedTime, AdaptorUserType.Lexis);
            if (string.IsNullOrEmpty(user.IdpSid))
            {
                user.IdpSid = idpSid;
                _unitOfWork.AdaptorUserRepository.Update(user);
                _unitOfWork.Save();
            }
        }

        // Build desired (groupId, roleId) set and resolved role map from UserOrg data
        var desiredRoles = new HashSet<(long GroupId, long RoleId)>();
        var projectRoleMap = new List<(List<AdaptorUserGroup> Groups, AdaptorUserRole Role)>();

        foreach (var lexisProject in lexisProjects)
        {
            var groupsWithProject = userLEXISGroups
                .Where(x => lexisProject.ProjectResourceNames.Any(a =>
                    string.Equals(a, x.Project.AccountingString, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            if (!groupsWithProject.Any()) continue;

            var roleNames = lexisProject.Permissions
                .Where(RoleMapping.MappingRoles.ContainsKey)
                .Select(s => RoleMapping.MappingRoles[s])
                .ToList();

            if (!roleNames.Any()) continue;

            var userRole = _unitOfWork.AdaptorUserRoleRepository.GetByRoleNames(roleNames);
            projectRoleMap.Add((groupsWithProject, userRole));

            foreach (var group in groupsWithProject)
                desiredRoles.Add((group.Id, (long)userRole.RoleType));
        }

        if (desiredRoles.Count == 0)
            throw new AuthenticationTypeException("NoUserGroup", user.Username);

        // Compare desired roles with current active roles already in DB
        var currentActiveRoles = user.AdaptorUserUserGroupRoles
            .Where(r => !r.IsDeleted)
            .Select(r => (r.AdaptorUserGroupId, r.AdaptorUserRoleId))
            .ToHashSet();

        if (desiredRoles.SetEquals(currentActiveRoles))
        {
            // Roles are identical — skip DB write to avoid row-lock contention under concurrent load
            _logger.LogDebug($"LEXIS AAI: Roles for user \"{user.Username}\" unchanged — skipping DB synchronization.");
            return user;
        }

        _logger.LogInformation($"LEXIS AAI: Roles for user \"{user.Username}\" changed — synchronizing to DB.");

        // Soft-delete all current roles and re-apply desired set
        user.AdaptorUserUserGroupRoles.ForEach(f =>
        {
            f.IsDeleted = true;
            f.ModifiedAt = changedTime;
        });

        foreach (var (groups, role) in projectRoleMap)
        {
            foreach (var group in groups)
                user.CreateSpecificUserRoleForUser(group, role.RoleType);
        }

        await _unitOfWork.SaveAsync();
        
        return user;
    }

    /// <summary>
    ///     Get existing or create new HEAppE user, from the OpenId credentials.
    /// </summary>
    /// <param name="decodedAccessToken">Decoded OpenId access token.</param>
    /// <returns>Newly created or existing HEAppE account.</returns>
    private AdaptorUser GetOrRegisterNewOpenIdUser(UserOpenId openIdUser)
    {
        var changedTime = DateTime.UtcNow;
        var user = _unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
    
        if (user is null)
        {
            try
            {
                user = CreateUser(openIdUser.UserName, openIdUser.Email, changedTime, AdaptorUserType.OpenId);
                _logger.LogInformation($"OpenId: Created new HEAppE account for user: \"{user}\"");
            }
            catch (Exception)
            {
                user = _unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
                if (user is null) throw;
            }
        }

        var hasUserGroup = false;

        user.AdaptorUserUserGroupRoles.ForEach(f =>
        {
            f.IsDeleted = true;
            f.ModifiedAt = changedTime;
        });

        foreach (var project in openIdUser.Projects)
        {
            if (!TryGetUserGroupByName(project.HEAppEGroupName, out var openIdGroup))
            {
                _logger.LogWarning($"OpenId: User group(\"{project.HEAppEGroupName}\") does not exist in HEAppE database!");
                continue;
            }

            var userRole = _unitOfWork.AdaptorUserRoleRepository.GetByRoleNames(project.Roles);
            user.CreateSpecificUserRoleForUser(openIdGroup, userRole.RoleType);

            hasUserGroup = true;
            _logger.LogInformation($"OpenId: User \"{user.Username}\" was added to group: \"{openIdGroup.Name}\"");
        }

        _unitOfWork.Save();
        return !hasUserGroup ? throw new AuthenticationTypeException("NoUserGroup", user.Username) : user;
    }

    /// <summary>
    ///     Create User
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="email">Email</param>
    /// <param name="changedTime">Changed time</param>
    /// <param name="adaptorUserType">UserType</param>
    /// <returns>User</returns>
    private AdaptorUser CreateUser(string username, string email, DateTime changedTime, AdaptorUserType adaptorUserType)
    {
        AdaptorUser user = new()
        {
            Username = username,
            Synchronize = false,
            Email = email,
            CreatedAt = changedTime,
            ModifiedAt = null,
            UserType = adaptorUserType
        };
        _unitOfWork.AdaptorUserRepository.Insert(user);
        _unitOfWork.Save();
        return user;
    }

    private AdaptorUser UpdateUser(AdaptorUser user, string username, string email, DateTime changedTime, AdaptorUserType adaptorUserType)
    {
        user.Username = username;
        user.Email = email;
        user.ModifiedAt = changedTime;
        user.UserType = adaptorUserType;
        _unitOfWork.AdaptorUserRepository.Update(user);
        _unitOfWork.Save();
        return user;
    }


    /// <summary>
    ///     Tries to get user group from database by its unique name.
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
    ///     Used for generation https://www.convertstring.com/en/Hash/SHA512
    /// </summary>
    /// <param name="credentials">Credentials</param>
    /// <returns></returns>
    /// <exception cref="InvalidAuthenticationCredentialsException"></exception>
    private string AuthenticateUserWithPassword(PasswordCredentials credentials)
    {
        var user = GetActiveUser(credentials.Username);

        var inputBytes = Encoding.UTF8.GetBytes(credentials.Password);
        var saltBytes = Encoding.UTF8.GetBytes(user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        var cipherBytes = inputBytes.Concat(saltBytes).ToArray();

        var hashBytes = SHA512.Create().ComputeHash(cipherBytes);
        StringBuilder sb = new();
        for (var i = 0; i < hashBytes.Length; i++) _ = sb.Append(hashBytes[i].ToString("X2"));
        var hash = sb.ToString();

        return hash != user.Password
            ? throw new InvalidAuthenticationCredentialsException("WrongCredentials", user.Username)
            : CreateSessionCode(user).UniqueCode;
    }

    private string AuthenticateUserWithDigitalSignature(DigitalSignatureCredentials credentials)
    {
        var user = GetActiveUser(credentials.Username);
        byte[] hash; // Hash of signed data

        // Hash data
        using (var hashAlg = SHA256.Create())
        {
            hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(credentials.SignedContent));
        }

        // Verify digital signature
        using var rsa = RSA.Create();
        ImportXmlPublicKey(rsa, user.PublicKey);
        
        return rsa.VerifyHash(hash, credentials.DigitalSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            ? CreateSessionCode(user).UniqueCode
            : throw new InvalidAuthenticationCredentialsException("WrongCredentials", user.Username);
    }

    private static void ImportXmlPublicKey(RSA rsa, string xmlString)
    {
        var parameters = new RSAParameters();
        
        var modulusMatch = System.Text.RegularExpressions.Regex.Match(xmlString, @"<Modulus>(.*?)</Modulus>");
        var exponentMatch = System.Text.RegularExpressions.Regex.Match(xmlString, @"<Exponent>(.*?)</Exponent>");
        
        if (!modulusMatch.Success || !exponentMatch.Success)
        {
            throw new InvalidOperationException("Invalid XML RSA public key format.");
        }
        
        parameters.Modulus = Convert.FromBase64String(modulusMatch.Groups[1].Value);
        parameters.Exponent = Convert.FromBase64String(exponentMatch.Groups[1].Value);
        
        rsa.ImportParameters(parameters);
    }

    private AdaptorUser GetActiveUser(string username)
    {
        _logger.LogInformation($"User \"{username}\" wants to authenticate to the system.");
        return _unitOfWork.AdaptorUserRepository.GetByName(username) ??
               throw new InvalidAuthenticationCredentialsException("WrongCredentials", username);
    }

    private SessionCode CreateSessionCode(AdaptorUser user)
    {
        var sessionCode = _unitOfWork.SessionCodeRepository.GetByUser(user);

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

    public IEnumerable<ProjectReference> ProjectsForCurrentUser(AdaptorUser loggedUser, IEnumerable<Project> projects)
    {
        List<ProjectReference> projectReferences = new();
        var validProjectIds = new HashSet<long>(
            projects?.Where(x => x != null).Select(x => x.Id) ?? Enumerable.Empty<long>()
        );
    
        var allGroupRoles = loggedUser.AdaptorUserUserGroupRoles
            .Where(x => !x.IsDeleted)
            .ToList();

        if (!allGroupRoles.Any())
        {
            return projectReferences;
        }

        var allActiveGroups = _unitOfWork.AdaptorUserGroupRepository
            .GetAllWithAdaptorUserGroupsAndActiveProjects()
            .ToDictionary(g => g.Id);

        var projectsToLoad = new List<Project>();
        var projectGroupRoles = new List<(AdaptorUserUserGroupRole GroupRole, Project Project)>();

        foreach (var groupRole in allGroupRoles)
        {
            if (allActiveGroups.TryGetValue(groupRole.AdaptorUserGroupId, out var group) && group.Project != null)
            {
                var project = group.Project;
                if (validProjectIds.Contains(project.Id))
                {
                    projectGroupRoles.Add((groupRole, project));
                    projectsToLoad.Add(project);
                }
            }
        }

        if (projectsToLoad.Any())
        {
            var uniqueProjectIds = projectsToLoad.Select(p => p.Id).Distinct().ToList();
            var templatesByProject = _unitOfWork.CommandTemplateRepository
                .GetCommandTemplatesByProjectIds(uniqueProjectIds)
                .GroupBy(t => t.ProjectId)
                .ToDictionary(g => g.Key ?? 0, g => g.ToList());

            foreach (var item in projectGroupRoles)
            {
                var project = item.Project;
                if (templatesByProject.TryGetValue(project.Id, out var templates))
                {
                    project.CommandTemplates = templates;
                }
                else
                {
                    project.CommandTemplates = new List<CommandTemplate>();
                }

                projectReferences.Add(new ProjectReference
                {
                    Role = item.GroupRole.AdaptorUserRole,
                    Project = project
                });
            }
        }

        return projectReferences;
    }

    #endregion
}
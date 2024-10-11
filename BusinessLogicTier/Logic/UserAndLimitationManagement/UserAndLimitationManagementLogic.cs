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
using HEAppE.OpenStackAPI;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.Utils;
using log4net;
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
        private readonly HttpClient _userOrgHttpClient;

        /// <summary>
        /// Lock for creating user
        /// </summary>
        private readonly object _lockCreateUserObj = new();

        /// <summary>
        /// Session code expiration in seconds
        /// </summary>
        private static readonly int _sessionExpirationSeconds = BusinessLogicConfiguration.SessionExpirationInSeconds;
        #endregion
        #region Constructors
        internal UserAndLimitationManagementLogic(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _userOrgHttpClient = httpClientFactory.CreateClient("userOrgApi");
        }
        #endregion
        #region Methods
        public AdaptorUser GetUserForSessionCode(string sessionCode)
        {
            SessionCode session = _unitOfWork.SessionCodeRepository.GetByUniqueCode(sessionCode);
            if (session is null)
            {
                throw new SessionCodeNotValidException("NotPresent", sessionCode);
            }

            if (IsSessionExpired(session))
            {
                throw new SessionCodeNotValidException("Expired", sessionCode, session.LastAccessTime.AddSeconds(_sessionExpirationSeconds).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }

            session.LastAccessTime = DateTime.UtcNow;
            _unitOfWork.SessionCodeRepository.Update(session);
            _unitOfWork.Save();
            return session.User;
        }

        public async Task<string> AuthenticateUserAsync(AuthenticationCredentials credentials)
        {
            return credentials switch
            {
                PasswordCredentials => AuthenticateUserWithPassword(credentials as PasswordCredentials),
                DigitalSignatureCredentials => AuthenticateUserWithDigitalSignature(credentials as DigitalSignatureCredentials),
                OpenIdCredentials => CreateSessionCode(await HandleOpenIdAuthenticationAsync(credentials as OpenIdCredentials)).UniqueCode,
                LexisCredentials => CreateSessionCode(await HandleTokenAsApiKeyAuthenticationAsync(credentials as LexisCredentials)).UniqueCode,
                _ => throw new AuthenticationTypeException("NotSupportedAuthentication")
            };
        }

        public async Task<AdaptorUser> AuthenticateUserToOpenIdAsync(OpenIdCredentials credentials)
        {
            _log.Info($"User \"{credentials.Username}\" wants to authenticate to the OpenStack.");

            AdaptorUser user = await HandleOpenIdAuthenticationAsync(credentials);
            return user;
        }

        /// <summary>
        /// Create new session/application credentials for authenticated user.
        /// </summary>
        /// <param name="adaptorUser">User with access to OpenStack part of the HEAppE.</param>
        /// <param name="projectId">Project Id</param>
        /// <returns>OpenStack application credentials.</returns>
        /// <exception cref="AuthenticationException">is throws, if OpenStack service is inaccessible.</exception>
        public async Task<ApplicationCredentialsDTO> AuthenticateOpenIdUserToOpenStackAsync(AdaptorUser adaptorUser, long projectId)
        {
            try
            {
                _log.Info($"OpenId: user \"{adaptorUser.Username}\" wants to authenticate to the OpenStack project \"{projectId}\".");

                if (!adaptorUser.Groups.Any(f => f.ProjectId == projectId))
                {
                    throw new AuthenticationTypeException("OpenStack-MissingCreateCredentialsPermission", projectId);
                }

                OpenStackProjectDTO openStackProject = GetOpenStackInstanceWithProjects(projectId);
                bool hasRequiredRole = adaptorUser.AdaptorUserUserGroupRoles.Any(x => (AdaptorUserRoleType)x.AdaptorUserRoleId == AdaptorUserRoleType.Submitter
                                                                                                    && x.AdaptorUserGroup.ProjectId == openStackProject.HEAppEProjectId
                                                                                                    && x.AdaptorUserGroup.Project.EndDate > DateTime.UtcNow);

                if (!hasRequiredRole)
                {
                    throw new InsufficientRoleException("MissingRoleForProject", AdaptorUserRoleType.Submitter.ToString(), openStackProject.HEAppEProjectId.Value);
                }

                OpenStack openStack = new(openStackProject.Domain.InstanceUrl);
                var authResponse = await openStack.AuthenticateAsync(openStackProject);
                ApplicationCredentialsDTO openStackCredentials = await openStack.CreateApplicationCredentialsAsync(adaptorUser.Username, authResponse);

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

                _log.Info($"Created new OpenStack 'session' (application credentials) for user \"{adaptorUser.Username}\".");
                return openStackCredentials;
            }
            catch (AuthenticationTypeException)
            {
                throw new AuthenticationTypeException("OpenStack-UnableToRetrieveCredentials");
            }
        }

        public AdaptorUserGroup GetDefaultSubmitterGroup(AdaptorUser loggedUser, long projectId)
        {
            return loggedUser.Groups.Where(x => x.ProjectId == projectId).FirstOrDefault() ?? loggedUser.Groups.FirstOrDefault() ?? _unitOfWork.AdaptorUserGroupRepository.GetDefaultSubmitterGroup();
        }

        public bool AuthorizeUserForJobInfo(AdaptorUser loggedUser, SubmittedJobInfo jobInfo)
        {
            return jobInfo.Submitter.Id == loggedUser.Id;
        }

        public bool AuthorizeUserForTaskInfo(AdaptorUser loggedUser, SubmittedTaskInfo taskInfo)
        {
            return taskInfo.Specification.JobSpecification.Submitter.Id == loggedUser.Id;
        }

        public IList<ResourceUsage> GetCurrentUsageAndLimitationsForUser(AdaptorUser loggedUser, IEnumerable<Project> projects)
        {
            IEnumerable<SubmittedJobInfo> notFinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork)
                                                                      .GetNotFinishedJobInfosForSubmitterId(loggedUser.Id);
            IEnumerable<ClusterNodeType> nodeTypes = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(_unitOfWork)
                                                                .ListClusterNodeTypes();

            IList<ResourceUsage> result = new List<ResourceUsage>(nodeTypes.Count());
            foreach (ClusterNodeType nodeType in nodeTypes)
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

        public IList<ProjectResourceUsage> CurrentUsageAndLimitationsForUserByProject(AdaptorUser loggedUser, IEnumerable<Project> projects)
        {
            IEnumerable<SubmittedJobInfo> allUserJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterId(loggedUser.Id);

            IList<ProjectResourceUsage> result = new List<ProjectResourceUsage>();
            foreach (Project project in projects)
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

                IList<CommandTemplate> projectCommandTemplates = _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(project.Id);
                IEnumerable<ClusterNodeType> nodeTypes = projectCommandTemplates.Select(x => x.ClusterNodeType).ToList().Distinct();
                foreach (ClusterNodeType nodeType in nodeTypes)
                {
                    IEnumerable<SubmittedTaskInfo> tasksAtNode = allUserJobs.SelectMany(x => x.Tasks).Where(x => x.NodeType == nodeType);
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
                result.Add(usage);
            }
            return result;
        }
        #endregion
        #region Private Methods
        private OpenStackProjectDTO GetOpenStackInstanceWithProjects(long? projectId)
        {
            DomainObjects.OpenStack.OpenStackProject project = _unitOfWork.OpenStackProjectRepository.GetOpenStackProjectByProjectId(projectId.Value)
                ?? throw new AuthenticationTypeException("OpenStack-NoOpenStackProject", projectId);

            DomainObjects.OpenStack.OpenStackAuthenticationCredentialProject projectCredentials = project.OpenStackAuthenticationCredentialProjects.FirstOrDefault(f => f.IsDefault) ?? project.OpenStackAuthenticationCredentialProjects.FirstOrDefault();

            return projectCredentials is null
                ? throw new AuthenticationTypeException("OpenStack-MissingCreateCredentialsPermission", projectId)
                : new OpenStackProjectDTO()
                {
                    Name = project.Name,
                    UID = project.UID,
                    HEAppEProjectId = project.AdaptorUserGroup.ProjectId,
                    ProjectDomain = new OpenStackProjectDomainDTO()
                    {
                        UID = project.OpenStackProjectDomain.UID,
                        Name = project.OpenStackProjectDomain.Name
                    },
                    Domain = new OpenStackDomainDTO()
                    {
                        UID = project.OpenStackDomain.UID,
                        Name = project.OpenStackDomain.Name,
                        InstanceUrl = project.OpenStackDomain.OpenStackInstance.InstanceUrl
                    },
                    Credentials = new OpenStackCredentialsDTO()
                    {
                        Id = projectCredentials.OpenStackAuthenticationCredential.UserId,
                        Username = projectCredentials.OpenStackAuthenticationCredential.Username,
                        Password = projectCredentials.OpenStackAuthenticationCredential.Password
                    }
                };
        }

        /// <summary>
        /// Check the provided OpenId tokens, refresh the token and the pass to logic to user retrieval.
        /// </summary>
        /// <param name="openIdCredentials">OpenId credentials.</param>
        /// <returns>New or existing user.</returns>
        private async Task<AdaptorUser> HandleOpenIdAuthenticationAsync(OpenIdCredentials openIdCredentials)
        {
            try
            {
                KeycloakOpenId openIdClient = new();
                ExternalAuthentication.DTO.JsonTypes.KeycloakTokenIntrospectionResult tokenIntrospectResult = await openIdClient.TokenIntrospectionAsync(openIdCredentials.OpenIdAccessToken);
                KeycloakOpenId.ValidateUserToken(tokenIntrospectResult);
                string offline_token = (await openIdClient.ExchangeTokenAsync(openIdCredentials.OpenIdAccessToken)).AccessToken;
                ExternalAuthentication.DTO.JsonTypes.KeycloakUserInfoResult userInfo = await openIdClient.GetUserInfoAsync(offline_token);
                return GetOrRegisterNewOpenIdUser(userInfo.Convert());
            }
            catch (AuthenticationTypeException)
            {
                throw new AuthenticationTypeException("InvalidToken");
            }
        }

        private async Task<AdaptorUser> HandleTokenAsApiKeyAuthenticationAsync(LexisCredentials lexisCredentials)
        {
            try
            {
                string requestUri = $"{LexisAuthenticationConfiguration.EndpointPrefix}{LexisAuthenticationConfiguration.ExtendedUserInfoEndpoint}";
                _userOrgHttpClient.DefaultRequestHeaders.Clear();
                _userOrgHttpClient.DefaultRequestHeaders.Add("X-Api-Token", lexisCredentials.OpenIdLexisAccessToken);
                UserInfoExtendedModel result = await _userOrgHttpClient.GetFromJsonAsync<UserInfoExtendedModel>(requestUri);
                return GetOrRegisterLexisCredentials(result);
            }
            catch (HttpRequestException)
            {
                throw new AuthenticationTypeException("InvalidToken");
            }
        }

        /// <summary>
        /// Get existing or create new HEAppE user, from the OpenId credentials.
        /// </summary>
        /// <param name="lexisUser"></param>
        /// <returns>Newly created or existing HEAppE account.</returns>
        /// <exception cref="AuthenticationTypeException"></exception>
        private AdaptorUser GetOrRegisterLexisCredentials(UserInfoExtendedModel lexisUser)
        {
            var lexisProjects = lexisUser.SystemRoles
                .Where(w => !string.IsNullOrEmpty(w.ProjectShortName))
                .Select(x => new
                {
                    x.ProjectShortName,
                    ProjectResourceNames = x.ProjectResources.Select(r => r.Name).Distinct(),
                    Permissions = x.SystemPermissionTypes
                });

            IEnumerable<AdaptorUserGroup> userLEXISGroups = _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndActiveProjects()
                .Where(w => w.Name.StartsWith(LexisAuthenticationConfiguration.HEAppEGroupNamePrefix));

            lock (_lockCreateUserObj)
            {
                DateTime changedTime = DateTime.UtcNow;
                AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetByName(lexisUser.UserName);
                if (user is null)
                {
                    user = CreateUser(lexisUser.UserName, lexisUser.Email, changedTime, AdaptorUserType.Lexis);
                    _log.Info($"LEXIS AAI: Created new HEAppE account for user: \"{user}\"");
                }

                bool hasUserGroup = false;

                //Set roles for group to IsDeleted
                user.AdaptorUserUserGroupRoles.ForEach(f => { f.IsDeleted = true; f.ModifiedAt = changedTime; });

                foreach (var lexisProject in lexisProjects)
                {
                    IEnumerable<AdaptorUserGroup> groupsWithProject = userLEXISGroups.Where(x => lexisProject.ProjectResourceNames.Any(a => string.Equals(a, x.Project.AccountingString, StringComparison.InvariantCultureIgnoreCase)));

                    if (groupsWithProject is null || !groupsWithProject.Any())
                    {
                        _log.Warn($"LEXIS AAI: User group with prefix \"{LexisAuthenticationConfiguration.HEAppEGroupNamePrefix}\" for Project Short Name \"{lexisProject.ProjectShortName}\" does not exist in HEAppE DB!");
                        continue;
                    }

                    IEnumerable<string> roleNames = lexisProject.Permissions.Where(RoleMapping.MappingRoles.ContainsKey).Select(s => RoleMapping.MappingRoles[s]);
                    if (roleNames is null || !roleNames.Any())
                    {
                        _log.Warn($"LEXIS AAI: Permissions for mapping is not correctly setup for Project Short Name \"{lexisProject.ProjectShortName}\"!");
                        continue;
                    }

                    AdaptorUserRole userRole = _unitOfWork.AdaptorUserRoleRepository.GetByRoleNames(roleNames);
                    foreach (AdaptorUserGroup prefixedGroup in groupsWithProject)
                    {
                        UpdateRoleForUserAndGroup(user, prefixedGroup, userRole);
                    }

                    hasUserGroup = true;
                    _log.Info($"LEXIS AAI: User \"{user.Username}\" for Project Short Name \"{lexisProject.ProjectShortName}\" was added to groups: \"{string.Join(',', groupsWithProject.Select(s => s.Name))}\"");
                    _unitOfWork.Save();
                }
                _unitOfWork.Save();

                return !hasUserGroup ? throw new AuthenticationTypeException("NoUserGroup", user.Username) : user;
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
                DateTime changedTime = DateTime.UtcNow;
                AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetByName(openIdUser.UserName);
                if (user is null)
                {
                    user = CreateUser(openIdUser.UserName, openIdUser.Email, changedTime, AdaptorUserType.OpenId);
                    _log.Info($"OpenId: Created new HEAppE account for user: \"{user}\"");
                }

                bool hasUserGroup = false;

                //Set roles for group to IsDeleted
                user.AdaptorUserUserGroupRoles.ForEach(f => { f.IsDeleted = true; f.ModifiedAt = changedTime; });

                foreach (ProjectOpenId project in openIdUser.Projects)
                {
                    if (!TryGetUserGroupByName(project.HEAppEGroupName, out AdaptorUserGroup openIdGroup))
                    {
                        _log.Warn($"OpenId: User group(\"{project.HEAppEGroupName}\") does not exist in HEAppE database!");
                        continue;
                    }

                    AdaptorUserRole userRole = _unitOfWork.AdaptorUserRoleRepository.GetByRoleNames(project.Roles);
                    UpdateRoleForUserAndGroup(user, openIdGroup, userRole);

                    hasUserGroup = true;
                    _log.Info($"OpenId: User \"{user.Username}\" was added to group: \"{openIdGroup.Name}\"");
                }

                _unitOfWork.Save();
                return !hasUserGroup ? throw new AuthenticationTypeException("NoUserGroup", user.Username) : user;
            }
        }

        /// <summary>
        /// Create User
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

        /// <summary>
        /// Update role for user and group
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="group">Group</param>
        /// <param name="userRole">UserRole</param>
        private static void UpdateRoleForUserAndGroup(AdaptorUser user, AdaptorUserGroup group, AdaptorUserRole userRole)
        {
            AdaptorUserUserGroupRole adaptorUserWithGroupRole = user.AdaptorUserUserGroupRoles.FirstOrDefault(f => f.AdaptorUserGroup == group);
            if (adaptorUserWithGroupRole is null)
            {
                user.CreateSpecificUserRoleForUser(group, userRole.RoleType);
            }
            else
            {
                if (adaptorUserWithGroupRole.AdaptorUserRole == userRole)
                {
                    adaptorUserWithGroupRole.IsDeleted = false;
                }

                user.CreateSpecificUserRoleForUser(group, userRole.RoleType);
                user.AdaptorUserUserGroupRoles.Remove(adaptorUserWithGroupRole);
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
        /// Used for generation https://www.convertstring.com/en/Hash/SHA512
        /// </summary>
        /// <param name="credentials">Credentials</param>
        /// <returns></returns>
        /// <exception cref="InvalidAuthenticationCredentialsException"></exception>
        private string AuthenticateUserWithPassword(PasswordCredentials credentials)
        {
            AdaptorUser user = GetActiveUser(credentials.Username);

            byte[] inputBytes = Encoding.UTF8.GetBytes(credentials.Password);
            byte[] saltBytes = Encoding.UTF8.GetBytes(user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            byte[] cipherBytes = inputBytes.Concat(saltBytes).ToArray();

            byte[] hashBytes = SHA512.Create().ComputeHash(cipherBytes);
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                _ = sb.Append(hashBytes[i].ToString("X2"));
            }
            string hash = sb.ToString();

            return hash != user.Password
                ? throw new InvalidAuthenticationCredentialsException("WrongCredentials", user.Username)
                : CreateSessionCode(user).UniqueCode;
        }

        private string AuthenticateUserWithDigitalSignature(DigitalSignatureCredentials credentials)
        {
            AdaptorUser user = GetActiveUser(credentials.Username);
            byte[] hash; // Hash of signed data

            // Hash data
            using (SHA256 hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(credentials.SignedContent));
            }

            // Verify digital signature
            using RSACryptoServiceProvider rsa = new(2048);
            // TODO: Verify
            rsa.FromXmlString(user.PublicKey);
            RSAPKCS1SignatureDeformatter rsaDeformatter = new(rsa);
            rsaDeformatter.SetHashAlgorithm("SHA256");
            return rsaDeformatter.VerifySignature(hash, credentials.DigitalSignature)
                ? CreateSessionCode(user).UniqueCode
                : throw new InvalidAuthenticationCredentialsException("WrongCredentials", user.Username);
        }

        private AdaptorUser GetActiveUser(string username)
        {
            _log.Info($"User \"{username}\" wants to authenticate to the system.");
            return _unitOfWork.AdaptorUserRepository.GetByName(username) ??
                throw new InvalidAuthenticationCredentialsException("WrongCredentials", username);
        }

        private SessionCode CreateSessionCode(AdaptorUser user)
        {
            SessionCode sessionCode = _unitOfWork.SessionCodeRepository.GetByUser(user);

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
            IEnumerable<AdaptorUserUserGroupRole> groupRoles = loggedUser.AdaptorUserUserGroupRoles.GroupBy(x => x.AdaptorUserGroup).Select(g => g.OrderBy(x => x.AdaptorUserRoleId).First());
            foreach (AdaptorUserUserGroupRole groupRole in groupRoles)
            {
                Project project = _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndActiveProjects().FirstOrDefault(x => x.Id == groupRole.AdaptorUserGroupId)?.Project;
                //if project.Id is present in projects array, then it is a project that user has access to
                if (project is null || !projects.Any(x => x.Id == project.Id))
                {
                    continue;
                }
                IList<CommandTemplate> commandTemplates = _unitOfWork.CommandTemplateRepository.GetCommandTemplatesByProjectId(project.Id);
                project.CommandTemplates = commandTemplates.ToList();
                projectReferences.Add(new()
                {
                    Role = groupRole.AdaptorUserRole,
                    Project = project
                });
            }
            return projectReferences;
        }
        #endregion
    }
}
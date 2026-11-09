using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

/// <summary>
/// User and role source provider that extracts user identity, projects, and roles
/// directly from OpenID Connect token claims (e.g. Keycloak token introspection or raw JWT).
/// </summary>
public class OpenIdClaimsUserRoleSourceProvider : IUserRoleSourceProvider
{
    public UserRoleSourceType ProviderType => UserRoleSourceType.OpenIdClaims;

    public async Task<AdaptorUser> ResolveAndSyncUserAsync(
        ClaimsPrincipal? claimsPrincipal,
        string? rawToken,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        logger.LogInformation("[OpenIdClaimsProvider] Resolving user identity and role mappings from token claims.");

        var claims = ExtractAllClaims(claimsPrincipal, rawToken, logger);
        var username = ResolveClaimValue(claims, OpenIdClaimsConfiguration.UsernameClaim,
            ClaimTypes.Name, "preferred_username", "username");
        var email = ResolveClaimValue(claims, OpenIdClaimsConfiguration.EmailClaim,
            ClaimTypes.Email, "email");
        var idpSid = ResolveClaimValue(claims, "sub", ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(username))
        {
            if (!string.IsNullOrWhiteSpace(email))
                username = email;
            else if (!string.IsNullOrWhiteSpace(idpSid))
                username = idpSid;
            else
                throw new AuthenticationTypeException("InvalidToken");
        }

        var user = await GetOrRegisterUserAsync(username, email, idpSid, unitOfWork, logger);

        if (user.IsBlocked)
        {
            logger.LogWarning($"[OpenIdClaimsProvider] User '{user.Username}' is blocked.");
            throw new UnauthorizedAccessException($"User '{user.Username}' is blocked.");
        }

        await SynchronizeUserRolesAsync(user, claims, unitOfWork, logger);
        return user;
    }

    private static List<Claim> ExtractAllClaims(ClaimsPrincipal? principal, string? rawToken, ILogger logger)
    {
        var claims = new List<Claim>();
        if (principal?.Claims != null)
        {
            claims.AddRange(principal.Claims);
        }

        if (claims.Count == 0 && !string.IsNullOrWhiteSpace(rawToken))
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(rawToken))
                {
                    var jwt = tokenHandler.ReadJwtToken(rawToken);
                    claims.AddRange(jwt.Claims);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[OpenIdClaimsProvider] Failed to decode raw token as fallback.");
            }
        }

        return claims;
    }

    private static string? ResolveClaimValue(IEnumerable<Claim> claims, params string?[] candidateTypes)
    {
        foreach (var type in candidateTypes)
        {
            if (string.IsNullOrEmpty(type)) continue;
            var match = claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase));
            if (match != null && !string.IsNullOrWhiteSpace(match.Value))
            {
                return match.Value;
            }
        }
        return null;
    }

    private async Task<AdaptorUser> GetOrRegisterUserAsync(
        string username,
        string? email,
        string? idpSid,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        AdaptorUser? user = null;
        DateTime now = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(idpSid) && idpSid != Guid.Empty.ToString())
        {
            user = await unitOfWork.AdaptorUserRepository.GetByIdpSidIgnoreQueryFiltersAsync(idpSid);
        }

        if (user == null && !string.IsNullOrEmpty(username))
        {
            user = await unitOfWork.AdaptorUserRepository.GetByNameIgnoreQueryFiltersAsync(username);
        }

        if (user == null && !string.IsNullOrEmpty(email))
        {
            user = await unitOfWork.AdaptorUserRepository.GetByEmailIgnoreQueryFiltersAsync(email);
        }

        if (user == null)
        {
            if (!OpenIdClaimsConfiguration.AutoCreateUser)
            {
                logger.LogWarning($"[OpenIdClaimsProvider] User '{username}' does not exist and AutoCreateUser is false.");
                throw new AuthenticationTypeException("UserNotRegisteredInHEAppE", username);
            }

            logger.LogInformation($"[OpenIdClaimsProvider] Creating new AdaptorUser account for '{username}' (email: '{email}').");
            user = new AdaptorUser
            {
                Username = username,
                Email = email ?? string.Empty,
                IdpSid = idpSid,
                CreatedAt = now,
                ModifiedAt = null,
                UserType = AdaptorUserType.OpenId,
                Synchronize = false,
                IsDeleted = false
            };

            unitOfWork.AdaptorUserRepository.Insert(user);
            await unitOfWork.SaveAsync();
        }
        else
        {
            bool modified = false;
            if (!string.IsNullOrEmpty(idpSid) && user.IdpSid != idpSid)
            {
                user.IdpSid = idpSid;
                modified = true;
            }
            if (!string.IsNullOrEmpty(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                modified = true;
            }
            if (modified)
            {
                user.ModifiedAt = now;
                unitOfWork.AdaptorUserRepository.Update(user);
                await unitOfWork.SaveAsync();
            }
        }

        return user;
    }

    private async Task SynchronizeUserRolesAsync(
        AdaptorUser user,
        List<Claim> claims,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        var activeProjects = (await unitOfWork.ProjectRepository.GetAllActiveProjectsAsync()).ToList();
        var allGroups = (await unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndActiveProjectsAsync()).ToList();

        // Map projectId -> AdaptorUserGroup (prefer OpenID or default group)
        var projectGroupMap = allGroups
            .Where(g => g.ProjectId.HasValue)
            .GroupBy(g => g.ProjectId.Value)
            .ToDictionary(
                grp => grp.Key,
                grp => grp.FirstOrDefault(g => !string.IsNullOrEmpty(ExternalAuthConfiguration.HEAppEUserPrefix) && g.Name.StartsWith(ExternalAuthConfiguration.HEAppEUserPrefix))
                       ?? grp.FirstOrDefault(g => string.IsNullOrEmpty(LexisAuthenticationConfiguration.HEAppEGroupNamePrefix) || !g.Name.StartsWith(LexisAuthenticationConfiguration.HEAppEGroupNamePrefix))
                       ?? grp.First()
            );

        var desiredRoles = new HashSet<(long GroupId, long RoleId)>();
        var projectRoleAssignments = new List<(AdaptorUserGroup Group, AdaptorUserRoleType RoleType)>();

        var defaultRoleTypes = ResolveDefaultRoleTypes();

        if (OpenIdClaimsConfiguration.AssignToAllActiveProjects)
        {
            logger.LogDebug("[OpenIdClaimsProvider] AssignToAllActiveProjects is enabled. Assigning user to all active projects.");
            foreach (var project in activeProjects)
            {
                if (projectGroupMap.TryGetValue(project.Id, out var group))
                {
                    foreach (var roleType in defaultRoleTypes)
                    {
                        desiredRoles.Add((group.Id, (long)roleType));
                        projectRoleAssignments.Add((group, roleType));
                    }
                }
            }
        }
        else
        {
            var groupClaimName = OpenIdClaimsConfiguration.GroupClaimName ?? "groups";
            var rawGroups = claims
                .Where(c => string.Equals(c.Type, groupClaimName, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .ToList();

            var regex = BuildGroupRegex();

            foreach (var rawGroup in rawGroups)
            {
                var match = regex.Match(rawGroup);
                if (!match.Success)
                {
                    logger.LogTrace("[OpenIdClaimsProvider] Group '{RawGroup}' did not match regex pattern.", rawGroup);
                    continue;
                }

                var projectName = match.Groups["project"].Success ? match.Groups["project"].Value : null;
                if (string.IsNullOrWhiteSpace(projectName))
                    continue;

                // Match project by AccountingString or Name
                var project = activeProjects.FirstOrDefault(p =>
                    string.Equals(p.AccountingString, projectName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));

                if (project == null)
                {
                    logger.LogWarning($"[OpenIdClaimsProvider] Project '{projectName}' from token group '{rawGroup}' not found in active HEAppE projects.");
                    continue;
                }

                if (!projectGroupMap.TryGetValue(project.Id, out var group))
                {
                    logger.LogWarning($"[OpenIdClaimsProvider] No AdaptorUserGroup found for project '{project.Name}' (ID: {project.Id}).");
                    continue;
                }

                var roleName = match.Groups["role"].Success ? match.Groups["role"].Value : null;
                var rolesToAssign = ResolveRolesForMatch(roleName, defaultRoleTypes);

                foreach (var roleType in rolesToAssign)
                {
                    desiredRoles.Add((group.Id, (long)roleType));
                    projectRoleAssignments.Add((group, roleType));
                }
            }
        }

        if (desiredRoles.Count == 0)
        {
            logger.LogWarning($"[OpenIdClaimsProvider] No matching projects/roles found for user '{user.Username}'.");
            throw new AuthenticationTypeException("NoUserGroup", user.Username);
        }

        // Optimistic check: if roles in DB already match desiredRoles, skip write to avoid row locks
        var currentActiveRoles = user.AdaptorUserUserGroupRoles
            .Where(r => !r.IsDeleted)
            .Select(r => (r.AdaptorUserGroupId, r.AdaptorUserRoleId))
            .ToHashSet();

        if (desiredRoles.SetEquals(currentActiveRoles))
        {
            logger.LogDebug($"[OpenIdClaimsProvider] Roles for user '{user.Username}' unchanged. Skipping DB update.");
            return;
        }

        logger.LogInformation($"[OpenIdClaimsProvider] Roles for user '{user.Username}' changed. Synchronizing to DB ({desiredRoles.Count} roles).");
        DateTime changeTime = DateTime.UtcNow;

        // Soft-delete removed roles
        foreach (var r in user.AdaptorUserUserGroupRoles.Where(r => !r.IsDeleted && !desiredRoles.Contains((r.AdaptorUserGroupId, r.AdaptorUserRoleId))))
        {
            r.IsDeleted = true;
            r.ModifiedAt = changeTime;
        }

        // Add or undelete new roles
        foreach (var (group, roleType) in projectRoleAssignments)
        {
            user.CreateSpecificUserRoleForUser(group, roleType);
        }

        await unitOfWork.SaveAsync();
    }

    private static Regex BuildGroupRegex()
    {
        if (!string.IsNullOrWhiteSpace(OpenIdClaimsConfiguration.GroupRegexPattern))
        {
            return new Regex(OpenIdClaimsConfiguration.GroupRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(OpenIdClaimsConfiguration.NotationPattern))
        {
            var pattern = OpenIdClaimsConfiguration.NotationPattern;
            pattern = pattern
                .Replace("{project}", "(?<project>[a-zA-Z0-9_-]+)")
                .Replace("{role}", "(?<role>[a-zA-Z0-9]+)")
                .Replace("{root}", "(?<root>[a-zA-Z0-9_-]+)");
            if (!pattern.StartsWith("^")) pattern = "^" + pattern;
            if (!pattern.EndsWith("$")) pattern = pattern + "$";
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        // Default regex covering Terrabyte (/hpda/heappe-pr74qo-Submitter or /heappe/pr74qo-Submitter) and standard (/projects/PRJ/Role)
        return new Regex(@"^/(?:hpda/|heappe/)?(?:heappe-)?(?<project>[a-zA-Z0-9_-]+)-(?<role>[a-zA-Z0-9]+)$|^/(?:projects/|dss/)?(?<project>[a-zA-Z0-9_-]+)(?:/(?<role>[a-zA-Z0-9]+))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    private static List<AdaptorUserRoleType> ResolveDefaultRoleTypes()
    {
        var result = new List<AdaptorUserRoleType>();
        var configured = OpenIdClaimsConfiguration.DefaultRoles ?? new List<string>();

        foreach (var r in configured)
        {
            if (Enum.TryParse<AdaptorUserRoleType>(r, true, out var roleType))
            {
                result.Add(roleType);
            }
        }

        if (result.Count == 0)
        {
            result.Add(AdaptorUserRoleType.Submitter);
        }

        return result;
    }

    private static List<AdaptorUserRoleType> ResolveRolesForMatch(string? roleName, List<AdaptorUserRoleType> fallbackDefaultRoles)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return fallbackDefaultRoles;
        }

        // Check custom alias mappings first
        if (OpenIdClaimsConfiguration.RoleMapping != null &&
            OpenIdClaimsConfiguration.RoleMapping.TryGetValue(roleName, out var mappedRoleNames))
        {
            var mappedTypes = mappedRoleNames
                .Select(m => Enum.TryParse<AdaptorUserRoleType>(m, true, out var rt) ? (AdaptorUserRoleType?)rt : null)
                .Where(rt => rt.HasValue)
                .Select(rt => rt!.Value)
                .ToList();

            if (mappedTypes.Count > 0)
            {
                return mappedTypes;
            }
        }

        if (Enum.TryParse<AdaptorUserRoleType>(roleName, true, out var parsedRoleType))
        {
            return new List<AdaptorUserRoleType> { parsedRoleType };
        }

        return fallbackDefaultRoles;
    }
}

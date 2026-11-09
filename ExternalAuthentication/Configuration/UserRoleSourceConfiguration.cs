using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.ExternalAuthentication.Configuration;

/// <summary>
/// Source provider type for user identity and role resolution.
/// </summary>
public enum UserRoleSourceType
{
    UserOrg,
    OpenIdClaims,
    Database
}

/// <summary>
/// Configuration for selecting and customizing the user & role source provider.
/// </summary>
public class UserRoleSourceConfiguration
{
    public static UserRoleSourceType Provider { get; set; } = UserRoleSourceType.UserOrg;

    public UserRoleSourceType ProviderInstance
    {
        get => Provider;
        set => Provider = value;
    }

    public static OpenIdClaimsConfiguration OpenIdClaims { get; set; } = new();

    public OpenIdClaimsConfiguration OpenIdClaimsInstance
    {
        get => OpenIdClaims;
        set => OpenIdClaims = value;
    }

    public static DatabaseSourceConfiguration Database { get; set; } = new();

    public DatabaseSourceConfiguration DatabaseInstance
    {
        get => Database;
        set => Database = value;
    }
}

/// <summary>
/// Configuration for resolving users and roles directly from OpenID Connect token claims.
/// </summary>
public class OpenIdClaimsConfiguration
{
    public static bool AutoCreateUser { get; set; } = true;
    public bool AutoCreateUserInstance { get => AutoCreateUser; set => AutoCreateUser = value; }

    public static string UsernameClaim { get; set; } = "preferred_username";
    public string UsernameClaimInstance { get => UsernameClaim; set => UsernameClaim = value; }

    public static string EmailClaim { get; set; } = "email";
    public string EmailClaimInstance { get => EmailClaim; set => EmailClaim = value; }

    public static string GroupClaimName { get; set; } = "groups";
    public string GroupClaimNameInstance { get => GroupClaimName; set => GroupClaimName = value; }

    /// <summary>
    /// Regex pattern to extract project and optional role from group claim items.
    /// Named groups supported: &lt;project&gt; and &lt;role&gt;.
    /// Example: "^/(?:hpda/|heappe/)?(?:heappe-)?(?&lt;project&gt;[a-zA-Z0-9_-]+)-(?&lt;role&gt;[a-zA-Z0-9]+)$"
    /// </summary>
    public static string? GroupRegexPattern { get; set; }
    public string? GroupRegexPatternInstance { get => GroupRegexPattern; set => GroupRegexPattern = value; }

    /// <summary>
    /// Simplified notation pattern, e.g. "/projects/{project}/{role}" or "/hpda/heappe-{project}-{role}".
    /// Automatically converted to GroupRegexPattern if GroupRegexPattern is not explicitly set.
    /// </summary>
    public static string? NotationPattern { get; set; }
    public string? NotationPatternInstance { get => NotationPattern; set => NotationPattern = value; }

    /// <summary>
    /// Default roles assigned to the user for matched projects (or when no role is in the group).
    /// </summary>
    public static List<string> DefaultRoles { get; set; } = new() { "Submitter", "Reporter" };
    public List<string> DefaultRolesInstance { get => DefaultRoles; set => DefaultRoles = value; }

    /// <summary>
    /// Single default role property for backward compatibility with string configuration.
    /// </summary>
    public static string? DefaultRole
    {
        get => DefaultRoles.Count > 0 ? DefaultRoles[0] : null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && !DefaultRoles.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                DefaultRoles.Insert(0, value);
            }
        }
    }
    public string? DefaultRoleInstance { get => DefaultRole; set => DefaultRole = value; }

    /// <summary>
    /// When true, grants DefaultRoles across all active projects in HEAppE, delegating final account enforcement to Slurm.
    /// </summary>
    public static bool AssignToAllActiveProjects { get; set; } = false;
    public bool AssignToAllActiveProjectsInstance { get => AssignToAllActiveProjects; set => AssignToAllActiveProjects = value; }

    /// <summary>
    /// Role name mappings/aliases (e.g. "user" -> ["Submitter"], "manager" -> ["Manager"]).
    /// </summary>
    public static Dictionary<string, List<string>> RoleMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> RoleMappingInstance { get => RoleMapping; set => RoleMapping = value; }
}

/// <summary>
/// Configuration when user identity is authenticated via OIDC but group/role memberships are managed purely in HEAppE DB.
/// </summary>
public class DatabaseSourceConfiguration
{
    public static bool AutoCreateUser { get; set; } = true;
    public bool AutoCreateUserInstance { get => AutoCreateUser; set => AutoCreateUser = value; }

    public static bool AllowUnassignedUsers { get; set; } = false;
    public bool AllowUnassignedUsersInstance { get => AllowUnassignedUsers; set => AllowUnassignedUsers = value; }
}

using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.Configuration;

/// <summary>
///     KeyCloak settings
/// </summary>
public class ExternalAuthConfiguration
{
    #region Properties

    /// <summary>
    ///     Base URL
    /// </summary>
    public static string BaseUrl { get; set; }

    /// <summary>
    ///     Client protocol
    /// </summary>
    public static string Protocol { get; set; }

    /// <summary>
    ///     Realm name
    /// </summary>
    public static string RealmName { get; set; }

    /// <summary>
    ///     Client id/name
    /// </summary>
    public static string ClientId { get; set; }

    /// <summary>
    ///     Client Secret
    /// </summary>
    public static string SecretId { get; set; }

    /// <summary>
    ///     Client connection timeout in seconds
    /// </summary>
    public static double ConnectionTimeout { get; set; } = 15;

    /// <summary>
    ///     Allowed client Ids
    /// </summary>
    public static IEnumerable<string> AllowedClientIds { get; set; }

    /// <summary>
    ///     Mapping role from OpenId to HEAppE internal roles
    /// </summary>
    public static Dictionary<string, string> RoleMapping { get; set; }

    /// <summary>
    ///     Projects
    /// </summary>
    public static IEnumerable<ExternalAuthProjectConfiguration> Projects { get; set; }

    /// <summary>
    ///     User prefix in HEAppE DB
    /// </summary>
    public static string HEAppEUserPrefix { get; set; }

    public static LexisAuthenticationConfiguration LexisAuthenticationConfiguration { get; set; } = new();

    #endregion
}
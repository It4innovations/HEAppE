namespace HEAppE.ExternalAuthentication.KeyCloak;

public class KeycloakSettings
{
    /// <summary>
    ///     Keycloak server base url.
    /// </summary>
    public static string BaseUrl { get; set; }

    /// <summary>
    ///     Keycloak client id/name.
    /// </summary>
    public static string ClientId { get; set; }

    /// <summary>
    ///     Keycloak client protocol.
    /// </summary>
    public static string Protocol { get; set; }

    /// <summary>
    ///     Group name in HeAppE database.
    /// </summary>
    public static string HEAppEGroupName { get; set; }

    /// <summary>
    ///     Keycloak realm name.
    /// </summary>
    public static string RealmName { get; set; }

    /// <summary>
    ///     Home organization.
    /// </summary>
    public static string Organization { get; set; }
}
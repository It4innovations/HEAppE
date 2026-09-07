namespace HEAppE.ExternalAuthentication.Configuration;

public class LexisAuthenticationConfiguration
{
    public static string ApiVersion { get; set; } = "v2";
    public static bool UseBearerAuth { get; set; } = false;
    public static string ExtendedUserInfoEndpoint { get; set; } = "/api/UserInfo/Extended";
    public static string ExtendedUserInfoV2Endpoint { get; set; } = "/api/v2/users/me/extended";
    public static string CommandTemplatePermissions { get; set; } = "/api/Heappe/CommandTemplatePermissions/";
    public static string CommandTemplatePermissionsV2Endpoint { get; set; } = "/api/v2/heappe/command-template-permissions/";
    public static bool CheckCommandTemplatePermissions { get; set; } = false;
    public static string BaseAddress { get; set; }
    public static string EndpointPrefix { get; set; }
    public static RoleMapping RoleMapping { get; set; } = new();
    public static string HEAppEGroupNamePrefix { get; set; }
    public static string HEAppEUserPrefix { get; set; }
    public static double ConnectionTimeoutInSeconds { get; set; } = 10;
}
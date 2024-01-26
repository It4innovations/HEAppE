namespace HEAppE.ExternalAuthentication.Configuration
{
  public class LexisAuthenticationConfiguration
  {
    public static string ExtendedUserInfoEndpoint { get; set; }
    public static string BaseAddress { get; set; }
    public static string EndpointPrefix { get; set; }
    public static RoleMapping RoleMapping { get; set; } = new();
    public static string HEAppEGroupNamePrefix { get; set; }
    public static string HEAppEUserPrefix { get; set; }

  }
}
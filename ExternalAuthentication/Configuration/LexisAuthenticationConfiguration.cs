namespace HEAppE.ExternalAuthentication.Configuration
{
  public class LexisAuthenticationConfiguration
  {
    public string ExtendedUserInfoEndpoint { get; set; }
    public string BaseAddress { get; set; }
    public string EndpointPrefix { get; set; }
    public LexisRoleMapping RoleMapping { get; set; }
    public string HEAppEGroupNamePrefix { get; set; }
    public string HEAppEUserPrefix { get; set; }


    //public static Uri BaseAddressUri => new Uri(LexisAuthenticationConfiguration.BaseAddress);
    //public static Uri ExtendedUserInfoEndpointUri => new Uri($"{EndpointPrefix}/{ExtendedUserInfoEndpoint}");
  }
}
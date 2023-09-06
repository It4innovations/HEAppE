using System;

namespace HEAppE.BusinessLogicTier.Configuration;

public sealed class LexisAuthenticationConfiguration
{
  public const string configurationPath = "ExternalAuthenticationSettings:LexisAuthenticationConfiguration";

  public static string ExtendedUserInfoEndpoint { get; set; }
  public static string BaseAddress { get; set; }
  public static string EndpointPrefix { get; set; }
  public static LexisRoleMapping RoleMapping { get; set; }
  public static string HEAppEGroupNamePrefix { get; set; }
  public static string HEAppEUserPrefix { get; set; }


  public static Uri BaseAddressUri => new Uri(BaseAddress);
  public static Uri ExtendedUserInfoEndpointUri => new Uri($"{EndpointPrefix}/{ExtendedUserInfoEndpoint}");
}

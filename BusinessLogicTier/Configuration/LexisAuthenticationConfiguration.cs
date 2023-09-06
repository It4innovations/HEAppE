using System;

namespace HEAppE.BusinessLogicTier.Configuration;

public sealed class LexisAuthenticationConfiguration
{
  public const string configurationPath = "ExternalAuthenticationSettings:LexisAuthenticationConfiguration";
  public const string extendedUserInfoEndpoint = "userorg/api/UserInfo/Extended";
  public string BaseAddress { get; set; } = "http://api.dev.msad.it4i.lexis.tech/userorg";
  public LexisRoleMapping RoleMapping { get; set; } = new LexisRoleMapping() { Maintainer = "prj_list", Reporter = "prj_read", Submitter = "prj_write" };
  public string HEAppEGroupNamePrefix { get; set; } = "Lexis_";
  public string HEAppEUserPrefix { get; set; } = "Lexis_";


  public Uri BaseAddressUri => new Uri(BaseAddress);
}

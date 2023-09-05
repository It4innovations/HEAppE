﻿using System;

namespace HEAppE.BusinessLogicTier.Configuration;

public sealed class LexisAuthenticationConfiguration
{
  public const string configurationPath = "ExternalAuthenticationSettings:LexisAuthenticationConfiguration";
  public const string extendedUserInfoEndpoint = "/api/UserInfo/Extended";
  public string BaseAddress { get; set; } = "http://api.dev.msad.it4i.lexis.tech/userorg";
  public LexisRoleMapping RoleMapping { get; set; }
  public string HEAppEGroupNamePrefix { get; set; }
  public string HEAppEUserPrefix { get; set; }


  public Uri BaseAddressUri => new Uri(BaseAddress);
}

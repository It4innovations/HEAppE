using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.ExternalAuthentication.DTO;


public sealed record LexisUserInfo()
{
  public Guid Id { get; set; }
  public string KeycloakSid { get; set; }
  public string Name { get; set; }
  public string UserName { get; set; }
  public string Email { get; set; }
  public List<LexisUserRole> SystemRoles { get; set; } = new();


  public static UserOpenId ToUserOpenId(LexisUserInfo user)
  {
    var userOpenId = new UserOpenId
    {
      UserName = user.UserName,
      Email = user.Email,
      FamilyName = string.Empty,
      GivenName = string.Empty,
      Name = user.Name,
      Projects = user.SystemRoles.Select(x => x.ToProjectOpenId(x))
    }
  }
}
